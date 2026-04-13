#pragma once

// Room for two players.
// p1 -> white
// p2 -> black

struct Point // coordinate
{
    unsigned short x = 0;
    unsigned short y = 0;
};

class Room
{
private:
    Point p1Point; // p1 
    Point p2Point; // p2 

    int p1Id;
    int p2Id;

public:
    Room()
    {
        
    }


};
