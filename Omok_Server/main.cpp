#include "stdafx.h"
#include "Session.h"

HANDLE g_iocp_handle;
SOCKET g_server;

std::array<Session*, MAX_USER> Sessions{};

void WorkerThread(HANDLE iocp_hd)
{
    while (true)
    {
        DWORD num_bytes;
        ULONG_PTR key;
        WSAOVERLAPPED* over;
        BOOL ret;
        ret = GetQueuedCompletionStatus(iocp_hd, &num_bytes, &key, &over, INFINITE);
        if (ret == FALSE)
        {
            if (over == nullptr)
            {
                std::cout << "GetQueuedCompletionStatus failed with error: " <<  GetLastError() << std::endl;
                continue;
            }
            continue;
        }

        int player_id = static_cast<int>(key); 
        EXT_OVER* ext_over = reinterpret_cast<EXT_OVER*>(over);

        if (ext_over->ov == TASK_TYPE::ACCEPT)
        {
            SOCKET client_socket = ext_over->acceptSocket;

            int client_id = GenerateClientId();
            std::cout << "ACCEPT: socket=" << client_socket << " id=" << client_id << std::endl;
            
            if (client_id != -1)
            {
                Session* sess = new Session(); // ???? 
                // OnConnect에는 실제 AcceptEx로 만들어진 소켓을 전달해야 함
                sess->OnConnect(client_socket, client_id);
                Sessions[client_id] = sess;

                // AcceptEx로 수락된 소켓은 부모 소켓 컨텍스트를 업데이트해야 합니다.
                setsockopt(client_socket, SOL_SOCKET, SO_UPDATE_ACCEPT_CONTEXT,
                           reinterpret_cast<const char*>(&g_server), sizeof(g_server));

                HANDLE hp = CreateIoCompletionPort(reinterpret_cast<HANDLE>(client_socket), iocp_hd, static_cast<ULONG_PTR>(client_id), 0);
                if (hp == NULL)
                {
                    std::cerr << "CreateIoCompletionPort for client failed: " << GetLastError() << std::endl;
                    closesocket(client_socket);
                    delete sess;
                }
                else
                {
                    // Post initial recv
                    sess->PostRecv();
                }
            }
            else
            {
                std::cout << "Max user exceeded.\n";
                closesocket(client_socket);
            }

            // Post another AcceptEx to accept next connection
            EXT_OVER* ac_over = new EXT_OVER();
            ac_over->ov = TASK_TYPE::ACCEPT;
            ac_over->acceptSocket = WSASocket(AF_INET, SOCK_STREAM, 0, NULL, 0, WSA_FLAG_OVERLAPPED);
            if (ac_over->acceptSocket == INVALID_SOCKET)
            {
                std::cerr << "WSASocket(for accept) failed: " << WSAGetLastError() << std::endl;
                delete ac_over;
            }
            else
            {
                BOOL ok = AcceptEx(g_server, ac_over->acceptSocket, ac_over->wb_buf, 0,
                                   sizeof(SOCKADDR_IN) + 16, sizeof(SOCKADDR_IN) + 16,
                                   0, &ac_over->over);
                if (!ok)
                {
                    int err = WSAGetLastError();
                    if (err != WSA_IO_PENDING)
                    {
                        std::cerr << "AcceptEx failed: " << err << std::endl;
                        closesocket(ac_over->acceptSocket);
                        delete ac_over;
                    }
                    // WSA_IO_PENDING 이면 정상적으로 비동기 대기중이며 오버랩은 완료 시 처리됨
                }
            }

            // ext_over는 힙 할당(이 파일의 AcceptEx 포스트 경로들에서)되어 있으므로 해제
            delete ext_over;
        }
        else if (ext_over->ov == TASK_TYPE::RECV)
        {
            // num_bytes bytes were received into the session's recv buffer.
            if (player_id >= 0 && player_id < static_cast<int>(MAX_USER) && Sessions[player_id])
            {
                Session* sess = Sessions[player_id];
                // ext_over here should point to sess->recvOver (owned by session), do not delete
                sess->OnRecvCompleted(ext_over, num_bytes);
            }
            else
            {
                std::cerr << "RECV completion for unknown session id=" << player_id << std::endl;
            }
        }
        else if (ext_over->ov == TASK_TYPE::SEND)
        {
            delete ext_over;
        }
    }
}

int main()
{
    initialize_server();

    // Post first AcceptEx
    EXT_OVER* ac_over = new EXT_OVER();
    ac_over->ov = TASK_TYPE::ACCEPT;
    ac_over->acceptSocket = WSASocket(AF_INET, SOCK_STREAM, 0, NULL, 0, WSA_FLAG_OVERLAPPED);
    if (ac_over->acceptSocket == INVALID_SOCKET)
    {
        std::cerr << "WSASocket(for accept) failed: " << WSAGetLastError() << std::endl;
        delete ac_over;
    }
    else
    {
        BOOL ok = AcceptEx(g_server, ac_over->acceptSocket, ac_over->wb_buf, 0,
                           sizeof(SOCKADDR_IN) + 16, sizeof(SOCKADDR_IN) + 16,
                           0, &ac_over->over);
        if (!ok)
        {
            int err = WSAGetLastError();
            if (err != WSA_IO_PENDING)
            {
                std::cerr << "AcceptEx failed: " << err << std::endl;
                closesocket(ac_over->acceptSocket);
                delete ac_over;
            }
            // WSA_IO_PENDING이면 정상: 오버랩은 WorkerThread에서 해제/처리
        }
    }

    vector<thread> worker_threads;
    for (int i = 0; i < int(thread::hardware_concurrency()); ++i)
        worker_threads.emplace_back(WorkerThread, g_iocp_handle);
    for (auto& th : worker_threads)
        th.join();
    closesocket(g_server);
    WSACleanup();
}


void initialize_server()
{
    setlocale(LC_ALL, "korean");

    WSADATA wsaData;
    if (WSAStartup(MAKEWORD(2, 2), &wsaData) != 0)
        std::cout << "WSAStartup failed" << std::endl;

    g_server = WSASocket(AF_INET, SOCK_STREAM, 0, NULL, 0, WSA_FLAG_OVERLAPPED);
    if (g_server == INVALID_SOCKET)
        std::cout << "WSASocket failed" << std::endl;

    u_long mode = 1;
    if (ioctlsocket(g_server, FIONBIO, &mode) != NO_ERROR) {
        std::cout << "ioctlsocket failed" << std::endl;
    }

    SOCKADDR_IN serverAddr;
    memset(&serverAddr, 0, sizeof(serverAddr));
    serverAddr.sin_family = AF_INET;
    serverAddr.sin_port = htons(PORT_NUM);
    serverAddr.sin_addr.s_addr = INADDR_ANY;

    if (bind(g_server, reinterpret_cast<sockaddr*>(&serverAddr), sizeof(serverAddr)) == SOCKET_ERROR)
        std::cout << "bind failed" << std::endl;

    if (listen(g_server, SOMAXCONN) == SOCKET_ERROR)
        std::cout << "listen failed" << std::endl;

    g_iocp_handle = CreateIoCompletionPort(INVALID_HANDLE_VALUE, NULL, 0, 0);
    if (g_iocp_handle == NULL)
        std::cout << "CreateIoCompletionPort failed" << std::endl;

    if (CreateIoCompletionPort(reinterpret_cast<HANDLE>(g_server), g_iocp_handle, 0, 0) == NULL)
        std::cout << "CreateIoCompletionPort for server socket failed" << std::endl;
}
