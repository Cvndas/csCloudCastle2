using CloudLib;

namespace Client.src;

// Client-specific send/receive functions using CloudLib.SenderReceiver.cs

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
    public static (ServerFlags? serverFlag, byte[] payload) ReceiveMessageCancellable(NetworkStream stream, CancellationToken token)
    {
        ServerFlags? retServerFlag = null;
        byte[]? retPayload = null;

        object resultIsReadyLock = new object();
        bool resultIsReady = false;

        // ----------------------------- Reading data from the server ------------------------------------ //
        Task.Run(async () => {
            var receivedData = await SenderReceiver.ReceiveMessageCancellable(stream, token);
            retServerFlag = (ServerFlags)receivedData.flag;
            retPayload = receivedData.payload;

            lock (resultIsReadyLock) {
                resultIsReady = true;
                Monitor.Pulse(resultIsReadyLock);
            }
        });

        // ----------------------------------------------------------------------------------------------- //

        // ------------------------- SYNCHRONIZATION ------------------------------- //
        lock (resultIsReadyLock) {
            if (!resultIsReady) {
                Monitor.Wait(resultIsReadyLock);
            }
        }

        Debug.Assert(retServerFlag != null);
        Debug.Assert(retPayload != null);

        // ------------------------------------------------------------------------- //

        return (retServerFlag, retPayload);

    }

    public static void ClientSendFlag(NetworkStream stream, ClientFlags flag)
    {
        SenderReceiver.SendMessage(stream, (byte)flag, Array.Empty<byte>());
    }

    public static void ClientSendChatMessage(NetworkStream stream, string username, string body)
    {
        Debug.Assert(false);
    }

    public static ServerFlags ClientReceiveFlag(NetworkStream stream)
    {
        (byte flag, byte[] payload) receivedMessages = SenderReceiver.ReceiveMessage(stream);
        // The asserts are fine for the client, but the server should handle this by kicking the client.
        Debug.Assert(receivedMessages.payload.Length == 0);
        return (ServerFlags)receivedMessages.flag;
    }
}