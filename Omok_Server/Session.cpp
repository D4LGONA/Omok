#include "stdafx.h"
#include "Session.h"

bool Session::PostRecv()
{
    if (socket == INVALID_SOCKET) return false;

    // Ensure wsabuf points to internal buffer
    recvOver.wsabuf.len = BUFSIZE;
    recvOver.wsabuf.buf = recvOver.wb_buf;
    ZeroMemory(&recvOver.over, sizeof(recvOver.over));
    recvOver.ov = TASK_TYPE::RECV;

    DWORD flags = 0;
    DWORD bytesRecv = 0;
    int ret = WSARecv(socket, &recvOver.wsabuf, 1, &bytesRecv, &flags, &recvOver.over, nullptr);
    if (ret == SOCKET_ERROR)
    {
        int err = WSAGetLastError();
        if (err != WSA_IO_PENDING)
        {
            std::cerr << "WSARecv failed: " << err << std::endl;
            return false;
        }
    }
    return true;
}

void Session::OnRecvCompleted(EXT_OVER* over, DWORD bytesTransferred)
{
    if (bytesTransferred == 0)
    {
        // connection closed by peer
        OnDisconnect();
        return;
    }

    // copy received data into packetBuf
    if (bytesTransferred + savedSize <= PACKET_BUF_SIZE)
    {
        memcpy(packetBuf + savedSize, over->wb_buf, bytesTransferred);
        savedSize += static_cast<int>(bytesTransferred);
    }
    else
    {
        // overflow, reset
        savedSize = 0;
        ZeroMemory(packetBuf, sizeof(packetBuf));
        // still post next receive to continue
        PostRecv();
        return;
    }

    // 항상 다음 recv를 게시하여 수신 중단을 방지
    PostRecv();

    // Protocol.h에 따라 패킷 헤더는: unsigned short size; PACKET_ID packetId; ...
    // 따라서 먼저 size(2바이트)를 읽고 완전한 패킷이 도착했을 때 큐에 넣는다.
    while (savedSize >= static_cast<int>(sizeof(unsigned short) + sizeof(PACKET_ID)))
    {
        unsigned short pktSize = 0;
        memcpy(&pktSize, packetBuf, sizeof(pktSize));
        if (pktSize == 0 || pktSize > PACKET_BUF_SIZE)
        {
            // 이상한 크기이면 버퍼 초기화
            savedSize = 0;
            ZeroMemory(packetBuf, sizeof(packetBuf));
            return;
        }

        if (savedSize < static_cast<int>(pktSize))
        {
            // 아직 전체 패킷이 도착하지 않음
            break;
        }

        // 완전한 패킷을 메모리 큐에 보관
        {
            std::vector<char> pkt(packetBuf, packetBuf + pktSize);
            std::lock_guard<std::mutex> lk(recvQueueMutex);
            recvQueue.emplace(std::move(pkt));
        }

        // 버퍼에서 해당 패킷 제거
        int remain = savedSize - static_cast<int>(pktSize);
        if (remain > 0)
            memmove(packetBuf, packetBuf + pktSize, remain);
        savedSize = remain;
    }

    // (처리 소비자는 TryPopPacket를 통해 큐에서 패킷을 꺼내 처리)
}

bool Session::PostSend(const char* buf, int len)
{
    if (socket == INVALID_SOCKET) return false;

    EXT_OVER* sendOver = new EXT_OVER();
    // prepare send buffer and overlapped
    sendOver->wsabuf.len = len;
    sendOver->wsabuf.buf = sendOver->wb_buf;
    ZeroMemory(&sendOver->over, sizeof(sendOver->over));
    sendOver->ov = TASK_TYPE::SEND;
    memcpy(sendOver->wb_buf, buf, len);

    DWORD bytesSent = 0;
    int ret = WSASend(socket, &sendOver->wsabuf, 1, &bytesSent, 0, &sendOver->over, nullptr);
    if (ret == SOCKET_ERROR)
    {
        int err = WSAGetLastError();
        if (err != WSA_IO_PENDING)
        {
            std::cerr << "WSASend failed: " << err << std::endl;
            delete sendOver;
            return false;
        }
    }
    return true;
}

bool Session::TryPopPacket(std::vector<char>& out)
{
    std::lock_guard<std::mutex> lk(recvQueueMutex);
    if (recvQueue.empty()) return false;
    out = std::move(recvQueue.front());
    recvQueue.pop();
    return true;
}
