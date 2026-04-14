using System;
using System.Text;
using UnityEngine;

public static class PacketHandler
{
    public static void Handle(byte[] packet)
    {
        if (packet == null || packet.Length < 3)
            return;

        ushort size = BitConverter.ToUInt16(packet, 0);
        PACKET_ID packetId = (PACKET_ID)packet[2];

        switch (packetId)
        {
            case PACKET_ID.SC_LOGIN_RESULT:
                HandleLoginResult(packet);
                break;

            case PACKET_ID.SC_GAMEMATCHING:
                HandleGameMatching(packet);
                break;

            case PACKET_ID.SC_JOINROOM:
                HandleJoinRoom(packet);
                break;

            case PACKET_ID.SC_PLAYTURN:
                HandlePlayTurn(packet);
                break;

            case PACKET_ID.SC_GAMERESULT:
                HandleGameResult(packet);
                break;

            default:
                Debug.LogWarning("알 수 없는 패킷 ID: " + packetId);
                break;
        }
    }

    private static void HandleLoginResult(byte[] packet)
    {
        bool success = packet[3] != 0;
        Debug.Log("로그인 결과: " + success);
    }

    private static void HandleGameMatching(byte[] packet)
    {
        bool success = packet[3] != 0;
        Debug.Log("매칭 결과: " + success);
    }

    private static void HandleJoinRoom(byte[] packet)
    {
        string otherId = ReadFixedString(packet, 3, 10);
        Debug.Log("상대방 ID: " + otherId);
    }

    private static void HandlePlayTurn(byte[] packet)
    {
        ushort x = BitConverter.ToUInt16(packet, 3);
        ushort y = BitConverter.ToUInt16(packet, 5);
        Debug.Log($"상대 착수: ({x}, {y})");
    }

    private static void HandleGameResult(byte[] packet)
    {
        bool win = packet[3] != 0;
        Debug.Log("게임 결과 승리 여부: " + win);
    }

    private static string ReadFixedString(byte[] packet, int offset, int length)
    {
        int realLen = 0;
        while (realLen < length && packet[offset + realLen] != 0)
            realLen++;

        return Encoding.ASCII.GetString(packet, offset, realLen);
    }
}