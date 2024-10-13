namespace Server.src;

internal class ClientTimeoutException : Exception
{
    public ClientTimeoutException(string message) : base(message) 
    {

    }
}