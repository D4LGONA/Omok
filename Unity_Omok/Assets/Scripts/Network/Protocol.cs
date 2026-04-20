using System;
using System.Text;

public static class Protocol
{
    public const int MAX_ID_LENGTH = 10;

    public enum PACKET_ID : byte
    {
        // Client to Server
        CS_LOGIN = 0,
        CS_QUEUE,
        CS_PLAYTURN,
        CS_MATCHING_RESPONSE,

        // Server to Client
        SC_LOGIN_RESULT,
        SC_GAMEMATCHING,
        SC_JOINROOM,
        SC_PLAYTURN,
        SC_GAMERESULT
    }

    public enum LOGIN_RESULT : byte
    {
        LOGIN_SUCCESS = 0,
        LOGIN_FAIL
    }

    public enum QUEUE_STATE : byte
    {
        QUEUE_ENTER = 0,
        QUEUE_CANCEL
    }

    public enum MATCHING_STATE : byte
    {
        MATCH_FOUND = 0,
        MATCH_CANCELED
    }

    public enum GAME_RESULT : byte
    {
        WIN = 0,
        LOSE,
        WIN_TIMEOUT,
        LOSE_TIMEOUT,
        WIN_DISCONNECT,
        LOSE_DISCONNECT
    }

    public static class PacketSize
    {
        public const int CS_LOGIN = 2 + 1 + MAX_ID_LENGTH;
        public const int CS_QUEUE = 2 + 1 + 1;
        public const int CS_MATCHING_RESPONSE = 2 + 1;
        public const int CS_PLAYTURN = 2 + 1 + 2 + 2;

        public const int SC_LOGIN_RESULT = 2 + 1 + 1;
        public const int SC_GAMEMATCHING = 2 + 1 + 1;
        public const int SC_JOINROOM = 2 + 1 + MAX_ID_LENGTH + 1;
        public const int SC_PLAYTURN = 2 + 1 + 2 + 2 + 1;
        public const int SC_GAMERESULT = 2 + 1 + 1;
    }

    public static byte[] MakeLoginPacket(string id)
    {
        byte[] buffer = new byte[PacketSize.CS_LOGIN];
        int offset = 0;

        WriteUShort(buffer, ref offset, (ushort)PacketSize.CS_LOGIN);
        WriteByte(buffer, ref offset, (byte)PACKET_ID.CS_LOGIN);
        WriteFixedString(buffer, ref offset, id, MAX_ID_LENGTH);

        return buffer;
    }

    public static byte[] MakeQueuePacket(QUEUE_STATE queueState)
    {
        byte[] buffer = new byte[PacketSize.CS_QUEUE];
        int offset = 0;

        WriteUShort(buffer, ref offset, (ushort)PacketSize.CS_QUEUE);
        WriteByte(buffer, ref offset, (byte)PACKET_ID.CS_QUEUE);
        WriteByte(buffer, ref offset, (byte)queueState);

        return buffer;
    }

    public static byte[] MakeMatchingResponsePacket()
    {
        byte[] buffer = new byte[PacketSize.CS_MATCHING_RESPONSE];
        int offset = 0;

        WriteUShort(buffer, ref offset, (ushort)PacketSize.CS_MATCHING_RESPONSE);
        WriteByte(buffer, ref offset, (byte)PACKET_ID.CS_MATCHING_RESPONSE);

        return buffer;
    }

    public static byte[] MakePlayTurnPacket(ushort x, ushort y)
    {
        byte[] buffer = new byte[PacketSize.CS_PLAYTURN];
        int offset = 0;

        WriteUShort(buffer, ref offset, (ushort)PacketSize.CS_PLAYTURN);
        WriteByte(buffer, ref offset, (byte)PACKET_ID.CS_PLAYTURN);
        WriteUShort(buffer, ref offset, x);
        WriteUShort(buffer, ref offset, y);

        return buffer;
    }

    public static bool TryReadHeader(byte[] buffer, int length, out ushort size, out PACKET_ID packetId)
    {
        size = 0;
        packetId = 0;

        if (buffer == null || length < 3)
            return false;

        int offset = 0;
        size = ReadUShort(buffer, ref offset);
        packetId = (PACKET_ID)ReadByte(buffer, ref offset);
        return true;
    }

    public static bool TryReadLoginResult(byte[] buffer, int length, out LOGIN_RESULT result)
    {
        result = LOGIN_RESULT.LOGIN_FAIL;

        if (length < PacketSize.SC_LOGIN_RESULT)
            return false;

        int offset = 0;
        ushort size = ReadUShort(buffer, ref offset);
        PACKET_ID packetId = (PACKET_ID)ReadByte(buffer, ref offset);

        if (size != PacketSize.SC_LOGIN_RESULT || packetId != PACKET_ID.SC_LOGIN_RESULT)
            return false;

        result = (LOGIN_RESULT)ReadByte(buffer, ref offset);
        return true;
    }

    public static bool TryReadGameMatching(byte[] buffer, int length, out MATCHING_STATE result)
    {
        result = MATCHING_STATE.MATCH_CANCELED;

        if (length < PacketSize.SC_GAMEMATCHING)
            return false;

        int offset = 0;
        ushort size = ReadUShort(buffer, ref offset);
        PACKET_ID packetId = (PACKET_ID)ReadByte(buffer, ref offset);

        if (size != PacketSize.SC_GAMEMATCHING || packetId != PACKET_ID.SC_GAMEMATCHING)
            return false;

        result = (MATCHING_STATE)ReadByte(buffer, ref offset);
        return true;
    }

    public static bool TryReadJoinRoom(byte[] buffer, int length, out string otherId, out bool bMyTurn)
    {
        otherId = string.Empty;
        bMyTurn = false;

        if (length < PacketSize.SC_JOINROOM)
            return false;

        int offset = 0;
        ushort size = ReadUShort(buffer, ref offset);
        PACKET_ID packetId = (PACKET_ID)ReadByte(buffer, ref offset);

        if (size != PacketSize.SC_JOINROOM || packetId != PACKET_ID.SC_JOINROOM)
            return false;

        otherId = ReadFixedString(buffer, ref offset, MAX_ID_LENGTH);
        bMyTurn = ReadByte(buffer, ref offset) != 0;
        return true;
    }

    public static bool TryReadPlayTurn(byte[] buffer, int length, out ushort x, out ushort y, out bool bMyTurn)
    {
        x = 0;
        y = 0;
        bMyTurn = false;

        if (length < PacketSize.SC_PLAYTURN)
            return false;

        int offset = 0;
        ushort size = ReadUShort(buffer, ref offset);
        PACKET_ID packetId = (PACKET_ID)ReadByte(buffer, ref offset);

        if (size != PacketSize.SC_PLAYTURN || packetId != PACKET_ID.SC_PLAYTURN)
            return false;

        x = ReadUShort(buffer, ref offset);
        y = ReadUShort(buffer, ref offset);
        bMyTurn = ReadByte(buffer, ref offset) != 0;
        return true;
    }

    public static bool TryReadGameResult(byte[] buffer, int length, out GAME_RESULT result)
    {
        result = GAME_RESULT.LOSE;

        if (length < PacketSize.SC_GAMERESULT)
            return false;

        int offset = 0;
        ushort size = ReadUShort(buffer, ref offset);
        PACKET_ID packetId = (PACKET_ID)ReadByte(buffer, ref offset);

        if (size != PacketSize.SC_GAMERESULT || packetId != PACKET_ID.SC_GAMERESULT)
            return false;

        result = (GAME_RESULT)ReadByte(buffer, ref offset);
        return true;
    }

    private static void WriteByte(byte[] buffer, ref int offset, byte value)
    {
        buffer[offset] = value;
        offset += 1;
    }

    private static void WriteUShort(byte[] buffer, ref int offset, ushort value)
    {
        byte[] temp = BitConverter.GetBytes(value);
        Buffer.BlockCopy(temp, 0, buffer, offset, 2);
        offset += 2;
    }

    private static void WriteFixedString(byte[] buffer, ref int offset, string value, int fixedLength)
    {
        byte[] temp = new byte[fixedLength];

        if (!string.IsNullOrEmpty(value))
        {
            byte[] src = Encoding.ASCII.GetBytes(value);
            int copyLen = Math.Min(src.Length, fixedLength);
            Buffer.BlockCopy(src, 0, temp, 0, copyLen);
        }

        Buffer.BlockCopy(temp, 0, buffer, offset, fixedLength);
        offset += fixedLength;
    }

    private static byte ReadByte(byte[] buffer, ref int offset)
    {
        byte value = buffer[offset];
        offset += 1;
        return value;
    }

    private static ushort ReadUShort(byte[] buffer, ref int offset)
    {
        ushort value = BitConverter.ToUInt16(buffer, offset);
        offset += 2;
        return value;
    }

    private static string ReadFixedString(byte[] buffer, ref int offset, int fixedLength)
    {
        string value = Encoding.ASCII.GetString(buffer, offset, fixedLength);
        offset += fixedLength;

        int nullIndex = value.IndexOf('\0');
        if (nullIndex >= 0)
            value = value.Substring(0, nullIndex);

        return value.TrimEnd('\0');
    }
}