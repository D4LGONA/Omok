#pragma once

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

#pragma pack(push, 1)

struct CS_LOGIN_PACKET
{
	unsigned short size;
	PACKET_ID packetId;
	char id[10]; // todo: 최대 글자수, constexpr으로 뺄 것
	char pw[10];
};

struct CS_QUEUE_PACKET // 매칭 관련 패킷
{
	unsigned short size;
	PACKET_ID packetId;
	bool bEnqueue = true; // 매칭 취소인지 확인
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
	bool bReturn = false; // todo: 만약.. 무르기를 넣는다면?
	unsigned short x;
	unsigned short y;
};

// -----------------------------------------------------------------------------------

struct SC_LOGIN_RESULT_PACKET
{
	unsigned short size;
	PACKET_ID packetId;
	bool bSuccess = true; // 로그인 성공했는지.
};

// 흠 여기를 큐잡힘 -> 잡혔다 보내고 -> 클라가 바로 응답 -> 그 응답 후 룸 생성.
// 이렇게 하면 안정적일 것 같군
struct SC_GAMEMATCHING_PACKET // 게임이 잡혔음 or 매칭취소.
{
	unsigned short size;
	PACKET_ID packetId;
	bool bSuccess = true; // false일 때 매칭취소된 것
};

struct SC_JOINROOM_PACKET
{
	unsigned short size;
	PACKET_ID packetId;
	char otherId[10];
};

struct SC_PLAYTURN_PACKET
{
	unsigned short size;
	PACKET_ID packetId;
	unsigned short otherX;
	unsigned short otherY;
};

struct SC_GAMERESULT_PACKET
{
	unsigned short size;
	PACKET_ID packetId;
	bool bWin = false;
};

#pragma pack(pop)