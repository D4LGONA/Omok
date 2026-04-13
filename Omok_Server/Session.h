#pragma once
#include "stdafx.h"
#include <string>
#include <atomic>
#include <winsock2.h>
#include "Protocol.h"
#include <queue>
#include <vector>
#include <mutex>

// Session lifecycle states
enum class SESSION_STATE
{
    FREE,
    CONNECTED,
    LOGGED_IN,
    MATCHING,
    IN_GAME
};

class Session
{
public:
    // Session identifier: this is the client id and is used as the key/index
    // into the global Sessions array.
    int sessionId = -1; // client id / session id (signed)

    SOCKET socket = INVALID_SOCKET;

    std::atomic<bool> connected = false;
    SESSION_STATE state = SESSION_STATE::FREE;

    std::string id; // ASCII user id

    static constexpr int PACKET_BUF_SIZE = 4096;
    char packetBuf[PACKET_BUF_SIZE]{};
    int savedSize = 0;

    EXT_OVER recvOver;

    // 수신 패킷을 임시 보관하는 메모리 큐
    std::mutex recvQueueMutex;
    std::queue<std::vector<char>> recvQueue;

public:
    Session() = default;

    // Note: actual generator is in stdafx.h; see implementation there.

    void Reset()
    {
        sessionId = -1;
        socket = INVALID_SOCKET;
        connected = false;
        state = SESSION_STATE::FREE;
        savedSize = 0;
        ZeroMemory(packetBuf, sizeof(packetBuf));
        id.clear();
        // clear queue
        std::lock_guard<std::mutex> lk(recvQueueMutex);
        while (!recvQueue.empty()) recvQueue.pop();
    }

    // Called when a new connection is established. Assigned id should be the
    // index in the global Sessions array.
    void OnConnect(SOCKET s, int assignedId)
    {
        socket = s;
        connected = true;
        state = SESSION_STATE::CONNECTED;
        sessionId = assignedId;
    }

    // Called to close / clean up the session
    void OnDisconnect()
    {
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

    // Post an overlapped receive for this session. Returns false on fatal error.
    bool PostRecv();

    // Post an overlapped send. The EXT_OVER used for send is allocated and
    // will be deleted by the IO completion worker when the send completes.
    bool PostSend(const char* buf, int len);

    // Called by IO worker when a receive completes on this session.
    void OnRecvCompleted(EXT_OVER* over, DWORD bytesTransferred);

    // pop a queued raw packet (thread-safe). Returns false if queue empty.
    bool TryPopPacket(std::vector<char>& out);

    // Convenience templated send for packet structs
    template<typename T>
    bool SendPacket(const T& pkt)
    {
        return PostSend(reinterpret_cast<const char*>(&pkt), static_cast<int>(sizeof(T)));
    }
};
