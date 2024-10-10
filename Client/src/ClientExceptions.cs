namespace Client.src;

class WrongUsernameException : Exception
{
    public WrongUsernameException(string message): base(message) {

    }
}
class InvalidStateTransitionException : Exception
{
    public InvalidStateTransitionException(string message) : base(message){

    }
}

class ExitProgram : Exception
{
    public ExitProgram(string message) : base(message){

    }
}