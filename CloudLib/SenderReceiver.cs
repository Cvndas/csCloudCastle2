using System.Net.Sockets;
using System.Diagnostics;
using CloudLib;

using static System.Diagnostics.Debug;

namespace CloudLib;

public static class SenderReceiver
{
    // --- PUBLIC --- 

    // -------------------- CLIENT ---------------------------- //
    public static (ServerFlags serverFlag, byte[] payload) ClientReceiveMessage(NetworkStream stream)
    {
        (byte flag, byte[] payload) = ReceiveMessage(stream);
        return ((ServerFlags)flag, payload);
    }

    /// <summary>
    /// Upon cancellation via CancellationToken, serverFlag is set to READ_CANCELED.
    /// </summary>
    public static async Task<(ServerFlags serverFlag, byte[] payload)> ClientReceiveMessageCancellable(NetworkStream stream, CancellationToken token)
    {
        var receivedData = await ReceiveMessageCancellable(stream, token);
        return ((ServerFlags)receivedData.flag, receivedData.payload);
    }

    public static void ClientSendFlag(NetworkStream stream, ClientFlags flag)
    {
        SendMessage(stream, (byte)flag, Array.Empty<byte>());
    }

    public static void ClientSendChatMessage(NetworkStream stream, string username, string body)
    {
        Assert(false);
    }

    public static ServerFlags ClientReceiveFlag(NetworkStream stream)
    {
        (byte flag, byte[] payload) receivedMessages = ReceiveMessage(stream);
        // The asserts are fine for the client, but the server should handle this by kicking the client.
        Assert(receivedMessages.payload.Length == 0);
        return (ServerFlags)receivedMessages.flag;
    }
    // -------------------------------------------------------- // 






    // -------------------- SERVER ---------------------------- //

    public static void ServerSendFlag(NetworkStream stream, ServerFlags flag)
    {
        SendMessage(stream, (byte)flag, Array.Empty<byte>());
    }

    public static void ServerSendString(NetworkStream stream, ServerFlags flag, string payloadString)
    {
        Assert(false);
    }

    public static void ServerSendChatMessage(NetworkStream stream, string body)
    {
        Assert(false);
    }

    public static void ServerReceiveFlag(NetworkStream stream)
    {
        // DON'T FORGET
        // "if payload > 0 " -> break connection to the client.
    }



    // ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++ //






    // --- PRIVATE ---

    private static void SendMessage(NetworkStream stream, byte flag, byte[] payload)
    {
        int payloadLen = payload.Length;
        byte[] sendBuffer = new byte[CloudProtocol.HEADER_LEN + payloadLen];
        byte[] payloadLengthBytes = BitConverter.GetBytes(payloadLen);
        sendBuffer[0] = flag;
        Array.Copy(payloadLengthBytes, 0, sendBuffer, CloudProtocol.FLAG_LEN, payloadLengthBytes.Count());
        Array.Copy(payload, 0, sendBuffer, CloudProtocol.HEADER_LEN, payloadLen);
        stream.Write(sendBuffer);
    }

    private static (byte flag, byte[] payload) ReceiveMessage(NetworkStream stream)
    {
        byte[] headerBuffer = new byte[CloudProtocol.HEADER_LEN];
        int headerBytesRead = 0;

        do {
            headerBytesRead += stream.Read(headerBuffer, 0, CloudProtocol.HEADER_LEN);
        } while (headerBytesRead < CloudProtocol.HEADER_LEN);

        byte flag = ProtocolHeader.GetGenericFlag(headerBuffer);
        int payloadLen = ProtocolHeader.GetPayloadLen(headerBuffer);
        if (payloadLen < 0) {
            return ((byte)ServerFlags.INVALID_FLAG, Array.Empty<byte>());
        }

        byte[] payloadBuffer = new byte[payloadLen];
        int payloadBytesRead = 0;
        do {
            payloadBytesRead += stream.Read(payloadBuffer, 0, payloadLen);
        } while (payloadBytesRead < payloadLen);

        return (flag, payloadBuffer);
    }


    /// <summary>
    /// Blocking read function that can be cancelled.
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="messageCount"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    private static async Task<(byte flag, byte[] payload)> ReceiveMessageCancellable(NetworkStream stream, CancellationToken token)
    {
        try {
            byte[] headerBuffer = new byte[CloudProtocol.HEADER_LEN];
            int headerBytesRead = 0;
            Task<int> headerBytesReadTask;

            do {
                headerBytesReadTask = stream.ReadAsync(headerBuffer, 0, CloudProtocol.HEADER_LEN, token);
                headerBytesRead += await (headerBytesReadTask);
            } while (headerBytesRead < CloudProtocol.HEADER_LEN && stream.DataAvailable);

            byte flag = ProtocolHeader.GetGenericFlag(headerBuffer);
            int payloadLen = ProtocolHeader.GetPayloadLen(headerBuffer);
            if (payloadLen < 0) {
                return ((byte)ServerFlags.INVALID_FLAG, Array.Empty<byte>());
            }

            byte[] payloadBuffer = new byte[payloadLen];
            int payloadBytesRead = 0;
            do {
                payloadBytesRead += await stream.ReadAsync(payloadBuffer, 0, payloadLen, token);
            } while (payloadBytesRead < payloadLen);

            return (flag, payloadBuffer);
        }
        catch (OperationCanceledException) {
            // Still consume all remaining data in the stream.
            Console.WriteLine("ReceiveMessagesCancellable canceled. Consuming remaining messages.");
            while (stream.DataAvailable) {
                ReceiveMessage(stream);
            }
            return ((byte)ServerFlags.READ_CANCELED, Array.Empty<byte>());
        }
    }

    private static (ServerFlags flag, byte[] payload) ParseServerMessage(byte[] message)
    {
        ServerFlags flag = ProtocolHeader.GetServerFlag(message);
        int payloadLen = ProtocolHeader.GetPayloadLen(message);
        byte[] payload = ProtocolHeader.GetPayload(message, payloadLen);
        return (flag, payload);
    }

    private static (ClientFlags flag, byte[] payload) ParseClientMessage(byte[] message)
    {
        ClientFlags flag = ProtocolHeader.GetClientFlag(message);
        int payloadLen = ProtocolHeader.GetPayloadLen(message);
        byte[] payload = ProtocolHeader.GetPayload(message, payloadLen);
        return (flag, payload);
    }
}