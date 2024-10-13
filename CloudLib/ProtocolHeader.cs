using System.Text;
using System.Net;

namespace CloudLib;
public static class ProtocolHeader
{
    /*
    ** ::: ::: ::: ::: Message structure ::: ::: :::  
    ** [1 byte : flagbyte] [4 byte : payloadlen] [var length payload]
    ** ::: ::: ::: ::: ----------------- ::: ::: ::: 
    */

    public static byte GetGenericFlag(byte[] message)
    {
        return message[0];
    }
    public static ClientFlags GetClientFlag(byte[] message)
    {
        return (ClientFlags)message[0];
    }

    public static ServerFlags GetServerFlag(byte[] message)
    {
        return (ServerFlags)message[0];
    }

    public static int GetPayloadLen(byte[] message)
    {
        return BitConverter.ToInt32(message, CloudProtocol.BYTE_LEN);
    }

    public static byte[] GetPayload(byte[] message, int payloadLen)
    {
        byte[] payload = new byte[payloadLen];
        Array.Copy(message, CloudProtocol.HEADER_LEN, payload, 0, payloadLen);
        return payload;
    }


}