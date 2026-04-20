#pragma once

#include <iostream>
#include <WS2tcpip.h>
#include <MSWSock.h>
#include <random>
#include <thread>
#include <concurrent_priority_queue.h>
#include <chrono>
#include <mutex>
#include <string>
#include <queue>
#include <unordered_map>
#include <atomic>
#include <array>

#pragma comment(lib, "WS2_32.lib")
#pragma comment(lib, "MSWSock.lib")
using namespace std;

constexpr unsigned short BUFSIZE = 256;
constexpr unsigned short MAX_USER = 1000;
constexpr unsigned short PORT_NUM = 6789;

inline std::atomic<int> user_count{0};

inline int GenerateClientId()
{
    int prev = user_count.fetch_add(1, std::memory_order_relaxed);
    if (prev >= static_cast<int>(MAX_USER))
        return -1;
    return prev;
}

extern std::array<class Session, MAX_USER> Sessions;

enum class TASK_TYPE
{
    RECV = 0,
    SEND = 1,
    ACCEPT = 2
};

class EXT_OVER
{
public:
    WSAOVERLAPPED over;
    WSABUF wsabuf;
    char wb_buf[BUFSIZE];
    TASK_TYPE ov;
    SOCKET acceptSocket = INVALID_SOCKET; // used for AcceptEx

    EXT_OVER() // recv
    {
        wsabuf.len = BUFSIZE;
        wsabuf.buf = wb_buf;
        ov = TASK_TYPE::RECV;
        ZeroMemory(&over, sizeof(over));
        ZeroMemory(&wb_buf, sizeof(wb_buf));
        acceptSocket = INVALID_SOCKET;
    }

    void setup_send(const char* pk, int len) // send
    {
        wsabuf.len = len;
        wsabuf.buf = wb_buf;
        ZeroMemory(&over, sizeof(over));
        ov = TASK_TYPE::SEND;
        memcpy(wb_buf, pk, len);
    }
};

#include "protocol.h"

inline const char* ToString(LOGIN_RESULT result)
{
    switch (result)
    {
    case LOGIN_SUCCESS: return "LOGIN_SUCCESS";
    case LOGIN_FAIL:    return "LOGIN_FAIL";
    default:            return "UNKNOWN_LOGIN_RESULT";
    }
}

inline const char* ToString(QUEUE_STATE state)
{
    switch (state)
    {
    case QUEUE_ENTER:  return "QUEUE_ENTER";
    case QUEUE_CANCEL: return "QUEUE_CANCEL";
    default:           return "UNKNOWN_QUEUE_STATE";
    }
}

inline const char* ToString(MATCHING_STATE state)
{
    switch (state)
    {
    case MATCH_FOUND:    return "MATCH_FOUND";
    case MATCH_CANCELED: return "MATCH_CANCELED";
    default:             return "UNKNOWN_MATCHING_STATE";
    }
}

inline const char* ToString(GAME_RESULT result)
{
    switch (result)
    {
    case WIN:             return "WIN";
    case LOSE:            return "LOSE";
    case WIN_TIMEOUT:     return "WIN_TIMEOUT";
    case LOSE_TIMEOUT:    return "LOSE_TIMEOUT";
    case WIN_DISCONNECT:  return "WIN_DISCONNECT";
    case LOSE_DISCONNECT: return "LOSE_DISCONNECT";
    default:              return "UNKNOWN_GAME_RESULT";
    }
}