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

#pragma comment(lib, "WS2_32.lib")
#pragma comment(lib, "MSWSock.lib")
using namespace std;

constexpr unsigned short BUFSIZE = 256;
constexpr unsigned short MAX_USER = 1000;
constexpr unsigned short PORT_NUM = 7777;

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

    EXT_OVER() // recv
    {
        wsabuf.len = BUFSIZE;
        wsabuf.buf = wb_buf;
        ov = TASK_TYPE::RECV;
        ZeroMemory(&over, sizeof(over));
        ZeroMemory(&wb_buf, sizeof(wb_buf));
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
