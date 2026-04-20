#pragma once

constexpr unsigned short MAX_ID_LENGTH = 10;

enum PACKET_ID : char
{
	// Client to Server
	CS_LOGIN = 0,
	CS_QUEUE, // 매칭 잡을때
	CS_PLAYTURN,
	CS_MATCHING_RESPONSE,

	// Server to Client
	SC_LOGIN_RESULT,
	SC_GAMEMATCHING,
	SC_JOINROOM,
	SC_PLAYTURN,
	SC_GAMERESULT
};

enum LOGIN_RESULT : char
{
	LOGIN_SUCCESS = 0,
	LOGIN_FAIL
};

enum QUEUE_STATE : char
{
	QUEUE_ENTER = 0,
	QUEUE_CANCEL
};

enum MATCHING_STATE : char
{
	MATCH_FOUND = 0,
	MATCH_CANCELED
};

enum GAME_RESULT : char
{
	WIN = 0,
	LOSE,
	WIN_TIMEOUT,
	LOSE_TIMEOUT,
	WIN_DISCONNECT,
	LOSE_DISCONNECT
};

#pragma pack(push, 1)

struct CS_LOGIN_PACKET
{
	unsigned short size;
	PACKET_ID packetId;
	char id[MAX_ID_LENGTH]; // todo: 최대 글자수, constexpr으로 뺄 것
};

struct CS_QUEUE_PACKET // 매칭 관련 패킷
{
	unsigned short size;
	PACKET_ID packetId;
	QUEUE_STATE queueState;
};

struct CS_MATCHING_RESPONSE_PACKET
{
	unsigned short size;
	PACKET_ID packetId;
};

struct CS_PLAYTURN_PACKET // 이번 턴의 돌을 어디에 넣는지
{
	unsigned short size;
	PACKET_ID packetId;
	unsigned short x;
	unsigned short y;
};

// -----------------------------------------------------------------------------------

struct SC_LOGIN_RESULT_PACKET
{
	unsigned short size;
	PACKET_ID packetId;
	LOGIN_RESULT loginResult; // 로그인 결과값
};

struct SC_MATCHING_PACKET // 게임이 잡혔음 or 매칭취소.
{
	unsigned short size;
	PACKET_ID packetId;
	MATCHING_STATE matchingResult;
};

struct SC_JOINROOM_PACKET
{
	unsigned short size;
	PACKET_ID packetId;
	char otherId[MAX_ID_LENGTH];
	bool bMyTurn;
};

struct SC_PLAYTURN_PACKET
{
	unsigned short size;
	PACKET_ID packetId;
	unsigned short x;
	unsigned short y;
	bool bMyTurn;
};

struct SC_GAMERESULT_PACKET
{
	unsigned short size;
	PACKET_ID packetId;
	GAME_RESULT gameResult;
};

#pragma pack(pop)