using CloudLib;

namespace Server.src;

internal class SSendRecv
{

    public static void SendFlag(NetworkStream stream, ServerFlags flag)
    {
        SendMessage(stream, (byte)flag, Array.Empty<byte>());
    }

    public static void SendString(NetworkStream stream, ServerFlags flag, string payloadString)
    {
        Debug.Assert(false);
    }

    public static void SendChatMessage(NetworkStream stream, string body)
    {
        Debug.Assert(false);
    }

    public static void ReceiveFlag(NetworkStream stream)
    {
        Debug.Assert(false);
        // DON'T FORGET
        // "if payload > 0 " -> break connection to the client.
    }
}