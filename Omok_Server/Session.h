#pragma once

/*
흐름 -> 로그인(아이디 입력) -> 매치메이킹 큐에 넣음 -> 큐잡힘 -> 게임.
*/

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
    int sessionId = -1;
    SOCKET socket = INVALID_SOCKET;

    std::atomic<bool> connected = false;
    SESSION_STATE state = SESSION_STATE::FREE;

    std::string id; // 영문 id

    static constexpr int PACKET_BUF_SIZE = 4096;
    char packetBuf[PACKET_BUF_SIZE]{};
    int savedSize = 0;

    EXT_OVER recvOver;

public:
    Session() = default;

    void Reset()
    {
        socket = INVALID_SOCKET;
        connected = false;
        state = SESSION_STATE::FREE;
        savedSize = 0;
        ZeroMemory(packetBuf, sizeof(packetBuf));
    }
};