#include "stdafx.h"
#include "Session.h"
#include "MatchManager.h"

void Session::SendLoginResult(bool success)
{
    SC_LOGIN_RESULT_PACKET pkt{};
    pkt.size = sizeof(SC_LOGIN_RESULT_PACKET);
    pkt.packetId = SC_LOGIN_RESULT;
	pkt.loginResult = success ? LOGIN_SUCCESS : LOGIN_FAIL;

    PostSend(reinterpret_cast<const char*>(&pkt), sizeof(pkt));
}

bool Session::PostRecv()
{
    if (socket == INVALID_SOCKET)
        return false;

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
        Disconnect();
        return;
    }

    if (bytesTransferred + savedSize > PACKET_BUF_SIZE)
    {
        std::cerr << "Recv buffer overflow. session id=" << id << std::endl;
        savedSize = 0;
        ZeroMemory(packetBuf, sizeof(packetBuf));

        if (!PostRecv())
            Disconnect();
        return;
    }

    memcpy(packetBuf + savedSize, over->wb_buf, bytesTransferred);
    savedSize += static_cast<int>(bytesTransferred);

    if (!PostRecv())
    {
        Disconnect();
        return;
    }

    while (savedSize >= static_cast<int>(sizeof(unsigned short) + sizeof(PACKET_ID)))
    {
        unsigned short pktSize = 0;
        memcpy(&pktSize, packetBuf, sizeof(pktSize));

        if (pktSize < sizeof(unsigned short) + sizeof(PACKET_ID) || pktSize > PACKET_BUF_SIZE)
        {
            std::cerr << "Invalid packet size: " << pktSize
                << " session id=" << id << std::endl;
            savedSize = 0;
            ZeroMemory(packetBuf, sizeof(packetBuf));
            Disconnect();
            return;
        }

        if (savedSize < static_cast<int>(pktSize))
            break;

        ProcessPacket(packetBuf, pktSize);

        int remain = savedSize - static_cast<int>(pktSize);
        if (remain > 0)
            memmove(packetBuf, packetBuf + pktSize, remain);

        savedSize = remain;
    }
}

bool Session::PostSend(const char* buf, int len)
{
    if (socket == INVALID_SOCKET)
        return false;

    if (buf == nullptr || len <= 0 || len > BUFSIZE)
    {
        std::cerr << "PostSend invalid args. len=" << len << std::endl;
        return false;
    }

    EXT_OVER* sendOver = new EXT_OVER();
    sendOver->setup_send(buf, len);

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

// 받은 패킷 처리하는 함수.
void Session::ProcessPacket(char* packet, int size)
{
    PACKET_ID packetId = static_cast<PACKET_ID>(packet[sizeof(unsigned short)]);

    switch (packetId)
    {
    case CS_LOGIN:
    {
        const CS_LOGIN_PACKET& pkt = *reinterpret_cast<CS_LOGIN_PACKET*>(packet);

        id = std::string(pkt.id, strnlen(pkt.id, sizeof(pkt.id)));
        state = SESSION_STATE::LOGIN_OK;

        std::cout << "[CS_LOGIN] sessionId=" << sessionId
            << " userId=" << id << std::endl;

        SendLoginResult(true);
        break;
    }
    case CS_QUEUE:
    {
        CS_QUEUE_PACKET& pkt = *reinterpret_cast<CS_QUEUE_PACKET*>(packet);
        HandleQueuePacket(*this, pkt);
        break;
    }
    case CS_MATCHING_RESPONSE:
    {
        CS_MATCHING_RESPONSE_PACKET& pkt = *reinterpret_cast<CS_MATCHING_RESPONSE_PACKET*>(packet);
        HandleMatchingResponse(*this, pkt);
        break;
    }
    case CS_PLAYTURN:
        std::cout << "[CS_PLAYTURN] session id=" << id << std::endl;
        break;

    default:
        std::cerr << "Unknown packet id: " << static_cast<int>(packetId)
            << " session id=" << id << std::endl;
        break;
    }
}