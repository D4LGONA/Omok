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
        Protocol.PACKET_ID packetId = (Protocol.PACKET_ID)packet[2];

        if (size > packet.Length)
        {
            Debug.LogWarning($"패킷 크기 이상함. header size={size}, actual={packet.Length}");
            return;
        }

        switch (packetId)
        {
            case Protocol.PACKET_ID.SC_LOGIN_RESULT:
                HandleLoginResult(packet);
                break;

            case Protocol.PACKET_ID.SC_GAMEMATCHING:
                HandleGameMatching(packet);
                break;

            case Protocol.PACKET_ID.SC_JOINROOM:
                HandleJoinRoom(packet);
                break;

            case Protocol.PACKET_ID.SC_PLAYTURN:
                HandlePlayTurn(packet);
                break;

            case Protocol.PACKET_ID.SC_GAMERESULT:
                HandleGameResult(packet);
                break;

            default:
                Debug.LogWarning("알 수 없는 패킷 ID: " + packetId);
                break;
        }
    }

    private static void HandleLoginResult(byte[] packet)
    {
        if (packet.Length < Protocol.PacketSize.SC_LOGIN_RESULT)
        {
            Debug.LogWarning("SC_LOGIN_RESULT 패킷 크기 부족");
            return;
        }

        Protocol.LOGIN_RESULT result = (Protocol.LOGIN_RESULT)packet[3];
        Debug.Log("로그인 결과: " + result);

        if (TitleSceneManager.Instance != null)
            TitleSceneManager.Instance.OnLoginResult(result);
    }

    private static void HandleGameMatching(byte[] packet)
    {
        if (packet.Length < Protocol.PacketSize.SC_GAMEMATCHING)
        {
            Debug.LogWarning("SC_GAMEMATCHING 패킷 크기 부족");
            return;
        }

        Protocol.MATCHING_STATE result = (Protocol.MATCHING_STATE)packet[3];
        Debug.Log("매칭 결과: " + result);

        if (TitleSceneManager.Instance != null)
            TitleSceneManager.Instance.OnMatchResult(result);
    }

    private static void HandleJoinRoom(byte[] packet)
    {
        if (packet.Length < Protocol.PacketSize.SC_JOINROOM)
        {
            Debug.LogWarning("SC_JOINROOM 패킷 크기 부족");
            return;
        }

        string otherId = ReadFixedString(packet, 3, Protocol.MAX_ID_LENGTH);
        bool bMyTurn = packet[3 + Protocol.MAX_ID_LENGTH] != 0;

        Debug.Log($"상대방 ID: {otherId}, 내 차례 여부: {bMyTurn}");

        if (TitleSceneManager.Instance != null)
            TitleSceneManager.Instance.OnJoinRoom(otherId, bMyTurn);
    }

    private static void HandlePlayTurn(byte[] packet)
    {
        if (packet.Length < Protocol.PacketSize.SC_PLAYTURN)
        {
            Debug.LogWarning("SC_PLAYTURN 패킷 크기 부족");
            return;
        }

        ushort x = BitConverter.ToUInt16(packet, 3);
        ushort y = BitConverter.ToUInt16(packet, 5);
        bool bMyTurn = packet[7] != 0;

        Debug.Log($"착수 좌표: ({x}, {y}), 이제 내 차례: {bMyTurn}");

    }

    private static void HandleGameResult(byte[] packet)
    {
        if (packet.Length < Protocol.PacketSize.SC_GAMERESULT)
        {
            Debug.LogWarning("SC_GAMERESULT 패킷 크기 부족");
            return;
        }

        Protocol.GAME_RESULT result = (Protocol.GAME_RESULT)packet[3];
        Debug.Log("게임 결과: " + result);

        switch (result)
        {
            case Protocol.GAME_RESULT.WIN:
                Debug.Log("승리");
                break;

            case Protocol.GAME_RESULT.LOSE:
                Debug.Log("패배");
                break;

            case Protocol.GAME_RESULT.WIN_TIMEOUT:
                Debug.Log("시간 초과 승리");
                break;

            case Protocol.GAME_RESULT.LOSE_TIMEOUT:
                Debug.Log("시간 초과 패배");
                break;

            case Protocol.GAME_RESULT.WIN_DISCONNECT:
                Debug.Log("상대 연결 종료로 승리");
                break;

            case Protocol.GAME_RESULT.LOSE_DISCONNECT:
                Debug.Log("연결 종료로 패배");
                break;
        }
    }

    private static string ReadFixedString(byte[] packet, int offset, int length)
    {
        int realLen = 0;
        while (realLen < length && packet[offset + realLen] != 0)
            realLen++;

        return Encoding.ASCII.GetString(packet, offset, realLen);
    }
}