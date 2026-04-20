#include "stdafx.h"
#include "Session.h"

constexpr int ACCEPT_DEPTH = 16;

HANDLE g_iocp_handle;
SOCKET g_server;

std::queue<int> g_matchingQueue;
std::mutex g_queueLock;

std::array<Session, MAX_USER> Sessions{};
std::array<EXT_OVER, ACCEPT_DEPTH> g_acceptContexts{};

void initialize_server();
bool PostAccept(EXT_OVER& ac_over);
void WorkerThread(HANDLE iocp_hd);

bool PostAccept(EXT_OVER& ac_over)
{
    ZeroMemory(&ac_over.over, sizeof(ac_over.over));
    ZeroMemory(ac_over.wb_buf, sizeof(ac_over.wb_buf));
    ac_over.ov = TASK_TYPE::ACCEPT;

    if (ac_over.acceptSocket != INVALID_SOCKET)
    {
        closesocket(ac_over.acceptSocket);
        ac_over.acceptSocket = INVALID_SOCKET;
    }

    ac_over.acceptSocket = WSASocket(AF_INET, SOCK_STREAM, 0, NULL, 0, WSA_FLAG_OVERLAPPED);
    if (ac_over.acceptSocket == INVALID_SOCKET)
    {
        std::cerr << "WSASocket(for accept) failed: " << WSAGetLastError() << std::endl;
        return false;
    }

    DWORD recv_bytes = 0;
    BOOL ok = AcceptEx(
        g_server,
        ac_over.acceptSocket,
        ac_over.wb_buf,
        0,
        sizeof(SOCKADDR_IN) + 16,
        sizeof(SOCKADDR_IN) + 16,
        &recv_bytes,
        &ac_over.over
    );

    if (!ok)
    {
        int err = WSAGetLastError();
        if (err != WSA_IO_PENDING)
        {
            std::cerr << "AcceptEx failed: " << err << std::endl;
            closesocket(ac_over.acceptSocket);
            ac_over.acceptSocket = INVALID_SOCKET;
            return false;
        }
    }

    return true;
}

void WorkerThread(HANDLE iocp_hd)
{
    while (true)
    {
        DWORD num_bytes = 0;
        ULONG_PTR key = 0;
        WSAOVERLAPPED* over = nullptr;

        BOOL ret = GetQueuedCompletionStatus(iocp_hd, &num_bytes, &key, &over, INFINITE);

        if (ret == FALSE)
        {
            if (over == nullptr)
            {
                std::cout << "GetQueuedCompletionStatus failed with error: "
                    << GetLastError() << std::endl;
                continue;
            }

            EXT_OVER* ext_over = reinterpret_cast<EXT_OVER*>(over);

            if (ext_over->ov == TASK_TYPE::ACCEPT)
            {
                std::cerr << "ACCEPT completion failed: " << GetLastError() << std::endl;
                PostAccept(*ext_over);
                continue;
            }
            else if (ext_over->ov == TASK_TYPE::RECV)
            {
                int player_id = static_cast<int>(key);
                if (player_id >= 0 && player_id < static_cast<int>(MAX_USER))
                {
                    Sessions[player_id].Disconnect();
                }
                continue;
            }
            else if (ext_over->ov == TASK_TYPE::SEND)
            {
                delete ext_over;
                continue;
            }

            continue;
        }

        EXT_OVER* ext_over = reinterpret_cast<EXT_OVER*>(over);

        if (ext_over->ov == TASK_TYPE::ACCEPT)
        {
            SOCKET client_socket = ext_over->acceptSocket;
            ext_over->acceptSocket = INVALID_SOCKET; // 세션으로 소유권 넘김

            int client_id = GenerateClientId();
            std::cout << "ACCEPT: socket=" << client_socket << " id=" << client_id << std::endl;

            if (client_id != -1)
            {
                Session& sess = Sessions[client_id];
                sess.Reset();
                sess.OnConnect(client_socket, client_id);

                if (setsockopt(client_socket,
                    SOL_SOCKET,
                    SO_UPDATE_ACCEPT_CONTEXT,
                    reinterpret_cast<const char*>(&g_server),
                    sizeof(g_server)) == SOCKET_ERROR)
                {
                    std::cerr << "SO_UPDATE_ACCEPT_CONTEXT failed: "
                        << WSAGetLastError() << std::endl;
                    closesocket(client_socket);
                    sess.Disconnect();
                }
                else
                {
                    HANDLE hp = CreateIoCompletionPort(
                        reinterpret_cast<HANDLE>(client_socket),
                        iocp_hd,
                        static_cast<ULONG_PTR>(client_id),
                        0
                    );

                    if (hp == NULL)
                    {
                        std::cerr << "CreateIoCompletionPort for client failed: "
                            << GetLastError() << std::endl;
                        closesocket(client_socket);
                        sess.Disconnect();
                    }
                    else
                    {
                        sess.PostRecv();
                    }
                }
            }
            else
            {
                std::cout << "Max user exceeded.\n";
                closesocket(client_socket);
            }

            PostAccept(*ext_over);
        }
        else if (ext_over->ov == TASK_TYPE::RECV)
        {
            int player_id = static_cast<int>(key);

            if (player_id >= 0 && player_id < static_cast<int>(MAX_USER))
            {
                Session& sess = Sessions[player_id];

                if (num_bytes == 0)
                {
                    sess.Disconnect();
                    continue;
                }

                sess.OnRecvCompleted(ext_over, num_bytes);
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

    for (auto& ac_over : g_acceptContexts)
    {
        PostAccept(ac_over);
    }

    std::vector<std::thread> worker_threads;
    for (int i = 0; i < static_cast<int>(std::thread::hardware_concurrency()); ++i)
        worker_threads.emplace_back(WorkerThread, g_iocp_handle);

    for (auto& th : worker_threads)
        th.join();

    closesocket(g_server);
    WSACleanup();
    return 0;
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
    if (ioctlsocket(g_server, FIONBIO, &mode) != NO_ERROR)
        std::cout << "ioctlsocket failed" << std::endl;

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