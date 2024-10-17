using CloudLib;

namespace Server.src;

internal class SMail
{

    public static void SendFlag(NetworkStream stream, ServerFlag flag)
    {
        SendMessage(stream, (byte)flag, Array.Empty<byte>());
    }

    public static void SendString(NetworkStream stream, ServerFlag flag, string payloadString)
    {
        Debug.Assert(false);
    }

    public static void SendChatMessage(NetworkStream stream, string body)
    {
        Debug.Assert(false);
    }

    public static ClientFlag ReceiveFlag(NetworkStream stream)
    {
        (byte flag, byte[] payload) receivedMessage = SenderReceiver.ReceiveMessage(stream);

        if (receivedMessage.payload.Length != 0) {
            Console.WriteLine("ReceiveFlag(): Expected payload length to be 0.");
            return ClientFlag.DISCONNECTED;
        }

        return (ClientFlag)receivedMessage.flag;
    }

    /// <summary>
    /// Does not throw an error. status of "Is canceled" or "disconnected" is in the flags. <br/>
    /// Check ServerFlags.DISCONNECTION and ServerFlags.READ_CANCELED
    /// </summary>
    public static (ClientFlag clientFlag, byte[] payload) ReceiveMessageCancellable(NetworkStream stream, CancellationToken token)
    {
        var receivedData = SenderReceiver.ReceiveMessageCancellable(stream, token).GetAwaiter().GetResult();
        return ((ClientFlag)receivedData.flag, receivedData.payload);
    }

    /// <summary>
    /// Check the ClientFlag for ServerFlags.DISCONNECTION or ServerFlags.READ_CANCELED via the helper 
    /// functions ClientDisconnected and ReadWasCancelled.
    /// </summary>
    public static ClientFlag ReceiveFlagCancellable(NetworkStream stream, CancellationToken token)
    {
        var receivedData = SenderReceiver.ReceiveMessageCancellable(stream, token).GetAwaiter().GetResult();
        return (ClientFlag) receivedData.flag; 
    }

    public static (ClientFlag clientFlag, string payload) ReceiveStringCancellable(NetworkStream stream, CancellationToken token)
    {
        (ClientFlag retFlag, byte[] payload) = ReceiveMessageCancellable(stream, token);
        return (retFlag, Encoding.UTF8.GetString(payload));
    }

    public static bool ClientDisconnected(ClientFlag clientFlag)
    {
        return ((ServerFlag)clientFlag == ServerFlag.DISCONNECTION);
    }
    public static bool ReadWasCancelled(ClientFlag clientFlag)
    {
        return ((ServerFlag)clientFlag == ServerFlag.READ_CANCELED);
    }
}