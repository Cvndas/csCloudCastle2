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

    public static ClientFlags ReceiveFlag(NetworkStream stream)
    {
        (byte flag, byte[] payload) receivedMessage = SenderReceiver.ReceiveMessage(stream);

        if (receivedMessage.payload.Length != 0) {
            Console.WriteLine("ReceiveFlag(): Expected payload length to be 0.");
            return ClientFlags.DISCONNECTED;
        }

        return (ClientFlags) receivedMessage.flag;
    }
}