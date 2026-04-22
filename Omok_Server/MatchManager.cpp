#include "stdafx.h"
#include "Session.h"
#include "MatchManager.h"
#include "Protocol.h"

std::queue<int> g_matchingQueue;
std::mutex g_queueLock;

static int PopValidQueuePlayer();
static void EnqueuePlayer(int sessionId);
static void CancelQueue(int sessionId);
static void TryMatchPlayers();
static void StartMatching(int p1SessionId, int p2SessionId);
static void CreateRoom(int p1SessionId, int p2SessionId);

static int PopValidQueuePlayer()
{
    while (!g_matchingQueue.empty())
    {
        int sessionId = g_matchingQueue.front();
        g_matchingQueue.pop();

        if (sessionId < 0 || sessionId >= static_cast<int>(MAX_USER))
            continue;

        Session& player = Sessions[sessionId];
        if (player.state == SESSION_STATE::IN_QUEUE && player.IsConnected())
            return sessionId;
    }

    return -1;
}

static void EnqueuePlayer(int sessionId)
{
    if (sessionId < 0 || sessionId >= static_cast<int>(MAX_USER))
        return;

    Session& player = Sessions[sessionId];

    std::lock_guard<std::mutex> lock(g_queueLock);

    if (player.state == SESSION_STATE::IN_QUEUE)
    {
        std::cout << "[QUEUE] already in queue: " << player.GetId() << std::endl;
        return;
    }

    if (player.state != SESSION_STATE::LOGIN_OK)
    {
        std::cout << "[QUEUE] invalid state for enqueue: " << player.sessionId << std::endl;
        return;
    }

    player.state = SESSION_STATE::IN_QUEUE;
    g_matchingQueue.push(sessionId);

    std::cout << "[QUEUE] enqueue: " << player.GetId() << std::endl;

    TryMatchPlayers();
}

static void CancelQueue(int sessionId)
{
    if (sessionId < 0 || sessionId >= static_cast<int>(MAX_USER))
        return;

    Session& player = Sessions[sessionId];

    std::lock_guard<std::mutex> lock(g_queueLock);

    if (player.state != SESSION_STATE::IN_QUEUE)
    {
        std::cout << "[QUEUE] cancel ignored: " << player.GetId() << std::endl;
        return;
    }

    player.state = SESSION_STATE::LOGIN_OK;

    std::cout << "[QUEUE] cancel: " << player.GetId() << std::endl;
}

static void TryMatchPlayers()
{
    while (true)
    {
        int p1Id = PopValidQueuePlayer();
        if (p1Id == -1)
            return;

        int p2Id = PopValidQueuePlayer();
        if (p2Id == -1)
        {
            g_matchingQueue.push(p1Id);
            return;
        }

        StartMatching(p1Id, p2Id);
    }
}

static void StartMatching(int p1SessionId, int p2SessionId)
{
    Session& p1 = Sessions[p1SessionId];
    Session& p2 = Sessions[p2SessionId];

    p1.state = SESSION_STATE::MATCH_FOUND;
    p2.state = SESSION_STATE::MATCH_FOUND;

    p1.matchedSessionId = p2SessionId;
    p2.matchedSessionId = p1SessionId;

    p1.bMatchResponse = false;
    p2.bMatchResponse = false;

    SC_MATCHING_PACKET pkt{};
    pkt.size = sizeof(pkt);
    pkt.packetId = SC_GAMEMATCHING;
    pkt.matchingResult = MATCH_FOUND;

    p1.PostSend(reinterpret_cast<const char*>(&pkt), sizeof(pkt));
    p2.PostSend(reinterpret_cast<const char*>(&pkt), sizeof(pkt));

    std::cout << "[MATCH] found: " << p1.GetId() << " vs " << p2.GetId() << std::endl;
}

void HandleQueuePacket(Session& player, const CS_QUEUE_PACKET& pkt)
{
    if (pkt.queueState == QUEUE_ENTER)
    {
        EnqueuePlayer(player.sessionId);
    }
    else if (pkt.queueState == QUEUE_CANCEL)
    {
        CancelQueue(player.sessionId);
    }
}

void HandleMatchingResponse(Session& player, const CS_MATCHING_RESPONSE_PACKET&)
{
    if (player.state != SESSION_STATE::MATCH_FOUND)
    {
        std::cout << "[MATCH] response ignored, invalid state" << std::endl;
        return;
    }

    player.bMatchResponse = true;

    int otherId = player.matchedSessionId;
    if (otherId < 0 || otherId >= static_cast<int>(MAX_USER))
        return;

    Session& other = Sessions[otherId];

    std::cout << "[MATCH] response from: " << player.GetId() << std::endl;

    if (other.state == SESSION_STATE::MATCH_FOUND && other.bMatchResponse)
    {
        player.state = SESSION_STATE::IN_ROOM;
        other.state = SESSION_STATE::IN_ROOM;

        CreateRoom(player.sessionId, other.sessionId);
    }
}

static void CreateRoom(int p1SessionId, int p2SessionId)
{
    Session& p1 = Sessions[p1SessionId];
    Session& p2 = Sessions[p2SessionId];

    SC_JOINROOM_PACKET pkt1{};
    pkt1.size = sizeof(pkt1);
    pkt1.packetId = SC_JOINROOM;
    memcpy(pkt1.otherId, p2.GetId().c_str(), min<size_t>(p2.GetId().size(), MAX_ID_LENGTH));
    pkt1.bMyTurn = true;

    SC_JOINROOM_PACKET pkt2{};
    pkt2.size = sizeof(pkt2);
    pkt2.packetId = SC_JOINROOM;
    memcpy(pkt2.otherId, p1.GetId().c_str(), min<size_t>(p1.GetId().size(), MAX_ID_LENGTH));
    pkt2.bMyTurn = false;

    p1.PostSend(reinterpret_cast<const char*>(&pkt1), sizeof(pkt1));
    p2.PostSend(reinterpret_cast<const char*>(&pkt2), sizeof(pkt2));

    std::cout << "[ROOM] created: " << p1.GetId() << " vs " << p2.GetId() << std::endl;
}

void HandleDisconnectMatchQueue(Session& player)
{
    if (player.state == SESSION_STATE::IN_QUEUE)
    {
        player.state = SESSION_STATE::LOGIN_OK;
        return;
    }

    if (player.state == SESSION_STATE::MATCH_FOUND)
    {
        int otherId = player.matchedSessionId;
        if (otherId >= 0 && otherId < static_cast<int>(MAX_USER))
        {
            Session& other = Sessions[otherId];

            if (other.state == SESSION_STATE::MATCH_FOUND)
            {
                other.state = SESSION_STATE::LOGIN_OK;
                other.matchedSessionId = -1;
                other.bMatchResponse = false;

                SC_MATCHING_PACKET pkt{};
                pkt.size = sizeof(pkt);
                pkt.packetId = SC_GAMEMATCHING;
                pkt.matchingResult = MATCH_CANCELED;

                other.PostSend(reinterpret_cast<const char*>(&pkt), sizeof(pkt));
            }
        }
    }
}