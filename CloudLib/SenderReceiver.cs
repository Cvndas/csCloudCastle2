using System.Net.Sockets;
using System.Diagnostics;
using CloudLib;

namespace CloudLib;

public static class SenderReceiver
{
    // --- PUBLIC --- 
    // -------------------- CLIENT ---------------------------- //
    public static void ClientSendFlag(NetworkStream stream, ClientFlags flag)
    {
        SendMessage(stream, (byte) flag, Array.Empty<byte>());
    }

    public static void ClientSendChatMessage(NetworkStream stream, string username, string body)
    {
    }

    public static ServerFlags ClientReceiveFlag(NetworkStream stream)
    {
        List<(byte flag, byte[] payload)> receivedMessages = ReceiveMessages(stream, 1);
        Debug.Assert(receivedMessages.Count == 1);
        Debug.Assert(receivedMessages[0].payload.Length == 0);
        return (ServerFlags)receivedMessages[0].flag;
    }

    public static (ServerFlags serverFlag, byte[] payload) ClientReceiveMessage(NetworkStream stream)
    {
        (byte flag, byte[] payload) = ReceiveMessages(stream, 1)[0];
        return ((ServerFlags) flag, payload);
    }

    // -------------------- SERVER ---------------------------- //

    public static void ServerSendFlag(NetworkStream stream, ServerFlags flag)
    {
        SendMessage(stream, (byte) flag, Array.Empty<byte>());
    }

    public static void ServerSendChatMessage(NetworkStream stream, string body)
    {

    }

    // TODO : Add more abstractions as required. There should be no mention of byte[] or headers outside of this file. 



    // ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++ //






    // --- PRIVATE ---

    private static void SendMessage(NetworkStream stream, byte flag, byte[] payload)
    {
        int payloadLen = payload.Length;
        byte[] sendBuffer = new byte[ProtocolConstants.HEADER_LEN + payloadLen];
        byte[] payloadLengthBytes = BitConverter.GetBytes(payloadLen);
        sendBuffer[0] = flag;
        Array.Copy(payloadLengthBytes, 0, sendBuffer, ProtocolConstants.FLAG_LEN, payloadLengthBytes.Count());
        Array.Copy(payload, 0, sendBuffer, ProtocolConstants.HEADER_LEN, payloadLen);
        stream.Write(sendBuffer);
    }

    private static List<(byte flag, byte[] payload)> ReceiveMessages(NetworkStream stream, int messageCount)
    {
        var ret = new List<(byte, byte[])>();
        for (int i = 0; i < messageCount; i++) {
            byte[] headerBuffer = new byte[ProtocolConstants.HEADER_LEN];
            stream.Read(headerBuffer, 0, ProtocolConstants.HEADER_LEN);
            byte flag = ProtocolHeader.GetGenericFlag(headerBuffer);
            int payloadLen = ProtocolHeader.GetPayloadLen(headerBuffer);
            if (payloadLen < 0) {
                return new List<(byte, byte[])>() { ((byte)ServerFlags.INVALID_FLAG, Array.Empty<byte>() ) };
            }

            byte[] payloadBuffer = new byte[payloadLen];
            int bytesRead = 0;
            do {
                bytesRead += stream.Read(payloadBuffer, 0, payloadLen);
            } while (bytesRead < payloadLen);

            ret.Add((flag, payloadBuffer));
        }
        return ret;
    }

    private static async Task<List<(byte flag, byte[] payload)>> CancellableReceiveMessages(NetworkStream stream, int messageCount)
    {
        // TODO Before dashboard
        return await Task.FromResult(new List<(byte flag, byte[] payload)>());
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