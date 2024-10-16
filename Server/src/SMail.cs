using CloudLib;

namespace Server.src;

internal class SMail
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

        return (ClientFlags)receivedMessage.flag;
    }

    /// <summary>
    /// Does not throw an error. status of "Is canceled" or "disconnected" is in the flags. <br/>
    /// Check ServerFlags.DISCONNECTION and ServerFlags.READ_CANCELED
    /// </summary>
    public static (ClientFlags clientFlag, byte[] payload) ReceiveMessageCancellable(NetworkStream stream, CancellationToken token)
    {
        var receivedData = SenderReceiver.ReceiveMessageCancellable(stream, token).GetAwaiter().GetResult();
        return ((ClientFlags)receivedData.flag, receivedData.payload);
    }

    public static (ClientFlags clientFlag, string payload) ReceiveStringCancellable(NetworkStream stream, CancellationToken token)
    {
        (ClientFlags retFlag, byte[] payload) = ReceiveMessageCancellable(stream, token);
        return (retFlag, Encoding.UTF8.GetString(payload));
    }

    public static bool ClientDisconnected(ClientFlags clientFlag)
    {
        return ((ServerFlags)clientFlag == ServerFlags.DISCONNECTION);
    }
    public static bool ReadWasCanceled(ClientFlags clientFlag)
    {
        return ((ServerFlags)clientFlag == ServerFlags.READ_CANCELED);
    }
}