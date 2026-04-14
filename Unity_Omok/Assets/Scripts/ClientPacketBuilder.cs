using System;
using System.Text;

public enum PACKET_ID : byte
{
    CS_LOGIN = 0,
    CS_QUEUE,
    CS_PLAYTURN,
    CS_MATCHING_RESPONSE,

    SC_LOGIN_RESULT,
    SC_GAMEMATCHING,
    SC_JOINROOM,
    SC_PLAYTURN,
    SC_GAMERESULT
}
public static class ClientPacketBuilder
{
    private const int FixedIdLength = 10;
    private const int FixedPwLength = 10;

    private static void WriteUShort(byte[] buffer, ref int offset, ushort value)
    {
        byte[] tmp = BitConverter.GetBytes(value);
        if (!BitConverter.IsLittleEndian)
            Array.Reverse(tmp);

        Buffer.BlockCopy(tmp, 0, buffer, offset, 2);
        offset += 2;
    }

    private static void WriteByte(byte[] buffer, ref int offset, byte value)
    {
        buffer[offset++] = value;
    }

    private static void WriteBool(byte[] buffer, ref int offset, bool value)
    {
        buffer[offset++] = value ? (byte)1 : (byte)0;
    }

    private static void WriteFixedString(byte[] buffer, ref int offset, string value, int fixedSize)
    {
        Array.Clear(buffer, offset, fixedSize);

        if (string.IsNullOrEmpty(value))
        {
            offset += fixedSize;
            return;
        }

        byte[] strBytes = Encoding.ASCII.GetBytes(value);
        int copyLen = Math.Min(strBytes.Length, fixedSize);
        Buffer.BlockCopy(strBytes, 0, buffer, offset, copyLen);
        offset += fixedSize;
    }

    public static byte[] MakeLogin(string id, string pw)
    {
        ushort size = (ushort)(2 + 1 + FixedIdLength + FixedPwLength);
        byte[] packet = new byte[size];

        int offset = 0;
        WriteUShort(packet, ref offset, size);
        WriteByte(packet, ref offset, (byte)PACKET_ID.CS_LOGIN);
        WriteFixedString(packet, ref offset, id, FixedIdLength);
        WriteFixedString(packet, ref offset, pw, FixedPwLength);

        return packet;
    }

    public static byte[] MakeQueue(bool enqueue)
    {
        ushort size = (ushort)(2 + 1 + 1);
        byte[] packet = new byte[size];

        int offset = 0;
        WriteUShort(packet, ref offset, size);
        WriteByte(packet, ref offset, (byte)PACKET_ID.CS_QUEUE);
        WriteBool(packet, ref offset, enqueue);

        return packet;
    }

    public static byte[] MakeMatchingResponse()
    {
        ushort size = (ushort)(2 + 1);
        byte[] packet = new byte[size];

        int offset = 0;
        WriteUShort(packet, ref offset, size);
        WriteByte(packet, ref offset, (byte)PACKET_ID.CS_MATCHING_RESPONSE);

        return packet;
    }

    public static byte[] MakePlayTurn(ushort x, ushort y, bool bReturn = false)
    {
        ushort size = (ushort)(2 + 1 + 1 + 2 + 2);
        byte[] packet = new byte[size];

        int offset = 0;
        WriteUShort(packet, ref offset, size);
        WriteByte(packet, ref offset, (byte)PACKET_ID.CS_PLAYTURN);
        WriteBool(packet, ref offset, bReturn);
        WriteUShort(packet, ref offset, x);
        WriteUShort(packet, ref offset, y);

        return packet;
    }
}