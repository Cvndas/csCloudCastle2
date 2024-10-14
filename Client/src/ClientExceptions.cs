namespace Client.src;

class WrongUsernameException : Exception
{
    public WrongUsernameException(string message) : base(message)
    {

    }
}
class InvalidStateTransitionException : Exception
{
    public InvalidStateTransitionException(string message) : base(message)
    {

    }
}

/// <summary>
/// Must cascade into a function that disposes of all connection resources.
/// </summary>
class ExitingProgramException : Exception
{
    // The timeout depends on the process. 
    // Authentication timeout is different than other timeouts, for example.
    public ExitingProgramException(string message) : base(message)
    {

    }
}

class ServerDisconnected : Exception
{
    public ServerDisconnected(string message) : base(message){

    }
}