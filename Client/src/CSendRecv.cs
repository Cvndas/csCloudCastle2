using CloudLib;

namespace Client.src;

/// <summary>
/// Client-specific send/receive functions using CloudLib.SenderReceiver.cs
/// </summary>
internal class CSendRecv
{

    /// <summary>
    /// Caller must check for SERVER_DISCONNECTED.
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    public static (ServerFlags serverFlag, byte[] payload) ReceiveMessage(NetworkStream stream)
    {
        try {
            (byte flag, byte[] payload) = SenderReceiver.ReceiveMessage(stream);
            return ((ServerFlags)flag, payload);
        }
        catch (IOException) {
            return (ServerFlags.DISCONNECTION, Array.Empty<byte>());
        }
    }


    /// <summary>
    /// May return ServerFlags.DISCONNECTED or ServerFlags.READ_CANCELED in the flag.
    /// </summary>
    public static (ServerFlags serverFlag, byte[] payload) ReceiveMessageCancellable(NetworkStream stream, CancellationToken token)
    {
        ServerFlags retServerFlag;
        byte[] retPayload;

        var receivedData = SenderReceiver.ReceiveMessageCancellable(stream, token).GetAwaiter().GetResult();

        retServerFlag = (ServerFlags)receivedData.flag;
        retPayload = receivedData.payload;

        return (retServerFlag, retPayload);
    }

    public static void SendFlag(NetworkStream stream, ClientFlags flag)
    {
        SenderReceiver.SendMessage(stream, (byte)flag, Array.Empty<byte>());
    }

    public static void SendString(ClientFlags flag, NetworkStream stream, String payloadString){
        return;
    }

    public static void SendChatMessage(NetworkStream stream, string username, string body)
    {
        Debug.Assert(false);
    }

    public static ServerFlags ReceiveFlag(NetworkStream stream)
    {
        (byte flag, byte[] payload) receivedMessage = SenderReceiver.ReceiveMessage(stream);

        // The asserts are fine for the client, but the server should handle this by kicking the client.
        if (receivedMessage.payload.Length != 0){
            Console.WriteLine("ReceiveFlag: Expected payload.Length to be 0.");
            return ServerFlags.DISCONNECTION;
        }
        return (ServerFlags)receivedMessage.flag;
    }
}