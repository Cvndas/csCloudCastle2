using CloudLib;

namespace Client.src;

/// <summary>
/// Client-specific send/receive functions using CloudLib.SenderReceiver.cs
/// </summary>
internal class CMail
{

    /// <summary>
    /// Caller must check for SERVER_DISCONNECTED.
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    public static (ServerFlag serverFlag, byte[] payload) ReceiveMessage(NetworkStream stream)
    {
        try {
            (byte flag, byte[] payload) = SenderReceiver.ReceiveMessage(stream);
            return ((ServerFlag)flag, payload);
        }
        catch (IOException) {
            return (ServerFlag.DISCONNECTION, Array.Empty<byte>());
        }
    }


    /// <summary>
    /// May return ServerFlags.DISCONNECTED or ServerFlags.READ_CANCELED in the flag.
    /// </summary>
    public static (ServerFlag serverFlag, byte[] payload) ReceiveMessageCancellable(NetworkStream stream, CancellationToken token)
    {
        ServerFlag retServerFlag;
        byte[] retPayload;

        var receivedData = SenderReceiver.ReceiveMessageCancellable(stream, token).GetAwaiter().GetResult();

        retServerFlag = (ServerFlag)receivedData.flag;
        retPayload = receivedData.payload;

        return (retServerFlag, retPayload);
    }

    /// <summary>
    /// Check the Flag for ServerFlags.DISCONNECTION or ServerFlags.READ_CANCELED
    /// </summary>
    public static ServerFlag ReceiveFlagCancellable(NetworkStream stream, CancellationToken token)
    {
        var receivedData = SenderReceiver.ReceiveMessageCancellable(stream, token).GetAwaiter().GetResult();
        return (ServerFlag)receivedData.flag;
    }

    public static void SendFlag(NetworkStream stream, ClientFlag flag)
    {
        SenderReceiver.SendMessage(stream, (byte)flag, Array.Empty<byte>());
    }

    public static void SendString(ClientFlag flag, NetworkStream stream, String payloadString)
    {
        byte[] payloadBytes = Encoding.UTF8.GetBytes(payloadString);
        SenderReceiver.SendMessage(stream, (byte)flag, payloadBytes);
    }

    public static void SendChatMessage(NetworkStream stream, string username, string body)
    {
        Debug.Assert(false);
    }

    /// <summary>
    /// Upon disconnection, flag is set to ServerFlags.DISCONNECTION <br/>
    /// Doesn't throw exceptions.
    /// </summary>
    public static ServerFlag ReceiveFlag(NetworkStream stream)
    {
        (byte flag, byte[] payload) receivedMessage = SenderReceiver.ReceiveMessage(stream);

        // The asserts are fine for the client, but the server should handle this by kicking the client.
        if (receivedMessage.payload.Length != 0) {
            Console.WriteLine("ReceiveFlag: Expected payload.Length to be 0.");
            return ServerFlag.DISCONNECTION;
        }
        return (ServerFlag)receivedMessage.flag;
    }
}