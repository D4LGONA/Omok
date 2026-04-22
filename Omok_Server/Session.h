#pragma once
#include "stdafx.h"
#include "Protocol.h"
#include "MatchManager.h"

// Session lifecycle states
enum class SESSION_STATE
{
    FREE = 0,
    CONNECTED,
    LOGIN_OK,
    IN_QUEUE,
    MATCH_FOUND,
    IN_ROOM
};

class Session
{
public:
    int sessionId = -1; // client id / session id (signed)

    SOCKET socket = INVALID_SOCKET;

    std::atomic<bool> connected = false;
    SESSION_STATE state = SESSION_STATE::FREE;

    std::string id; // user id
    int matchedSessionId = -1;
    bool bMatchResponse = false;

    static constexpr int PACKET_BUF_SIZE = 4096;
    char packetBuf[PACKET_BUF_SIZE]{};
    int savedSize = 0;

    EXT_OVER recvOver;

public:

    // 패킷 송신 함수들
    void SendLoginResult(bool success);


    Session() = default;

    void Reset()
    {
        matchedSessionId = -1;
        bMatchResponse = false;
        sessionId = -1;
        socket = INVALID_SOCKET;
        connected = false;
        state = SESSION_STATE::FREE;
        savedSize = 0;
        ZeroMemory(packetBuf, sizeof(packetBuf));
        id.clear();
    }

    void OnConnect(SOCKET s, int assignedId)
    {
        socket = s;
        connected = true;
        state = SESSION_STATE::CONNECTED;
        sessionId = assignedId;
    }

    // Called to close / clean up the session
    void Disconnect()
    {
        HandleDisconnectMatchQueue(*this);
        connected = false;
        if (socket != INVALID_SOCKET)
        {
            closesocket(socket);
            socket = INVALID_SOCKET;
        }
        Reset();
    }

    bool IsConnected() const { return connected.load(); }

    void SetId(const std::string& newId) { id = newId; }
    const std::string& GetId() const { return id; }

    bool PostRecv();
    bool PostSend(const char* buf, int len);
    void OnRecvCompleted(EXT_OVER* over, DWORD bytesTransferred);

    void ProcessPacket(char* packet, int size);

    template<typename T>
    bool SendPacket(const T& pkt)
    {
        return PostSend(reinterpret_cast<const char*>(&pkt), static_cast<int>(sizeof(T)));
    }
};
