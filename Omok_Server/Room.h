#pragma once


// 유저 두명이 겨루는 방.

struct Point // 좌표.
{
	unsigned short x = 0;
	unsigned short y = 0;
};

class Room
{
private:
	Point p1Point; // p1좌표
	Point p2Point; // p2좌표

	unsigned short p1Id;
	unsigned short p2Id;

public:
	Room()
	{
		
	}

};

