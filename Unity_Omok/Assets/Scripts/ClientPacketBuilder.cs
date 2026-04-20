using System;
using System.Text;

public static class ClientPacketBuilder
{
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

    public static byte[] MakeLogin(string id)
    {
        ushort size = (ushort)Protocol.PacketSize.CS_LOGIN;
        byte[] packet = new byte[size];

        int offset = 0;
        WriteUShort(packet, ref offset, size);
        WriteByte(packet, ref offset, (byte)Protocol.PACKET_ID.CS_LOGIN);
        WriteFixedString(packet, ref offset, id, Protocol.MAX_ID_LENGTH);

        return packet;
    }

    public static byte[] MakeQueue(Protocol.QUEUE_STATE queueState)
    {
        ushort size = (ushort)Protocol.PacketSize.CS_QUEUE;
        byte[] packet = new byte[size];

        int offset = 0;
        WriteUShort(packet, ref offset, size);
        WriteByte(packet, ref offset, (byte)Protocol.PACKET_ID.CS_QUEUE);
        WriteByte(packet, ref offset, (byte)queueState);

        return packet;
    }

    public static byte[] MakeMatchingResponse()
    {
        ushort size = (ushort)Protocol.PacketSize.CS_MATCHING_RESPONSE;
        byte[] packet = new byte[size];

        int offset = 0;
        WriteUShort(packet, ref offset, size);
        WriteByte(packet, ref offset, (byte)Protocol.PACKET_ID.CS_MATCHING_RESPONSE);

        return packet;
    }

    public static byte[] MakePlayTurn(ushort x, ushort y)
    {
        ushort size = (ushort)Protocol.PacketSize.CS_PLAYTURN;
        byte[] packet = new byte[size];

        int offset = 0;
        WriteUShort(packet, ref offset, size);
        WriteByte(packet, ref offset, (byte)Protocol.PACKET_ID.CS_PLAYTURN);
        WriteUShort(packet, ref offset, x);
        WriteUShort(packet, ref offset, y);

        return packet;
    }
}