namespace CloudLib;

/// <summary>
/// Class with helper functions to validate input on both server and client side. 
/// </summary>
public class MessageValidation
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="usernamePassword"></param>
    /// <returns>
    /// USERNAME_TOO_LONG<br/>
    /// USERNAME_TOO_SHORT<br/>
    /// PASSWORD_TOO_LONG<br/>
    /// PASSWORD_TOO_SHORT<br/>
    /// WRONG_MESSAGE_FORMAT<br/>
    /// OK<br/>
    ///</returns>
    public static MessageValidationResult ValidateUsernamePassword(string usernamePassword)
    {
        string[] chunked = usernamePassword.Split(" ");

        bool wrongMessageFormat = chunked.Length != 2;
        if (wrongMessageFormat){
            return MessageValidationResult.WRONG_MESSAGE_FORMAT;
        }

        string username = chunked[0]; 
        string password = chunked[1];

        wrongMessageFormat = username == "" || password == "";
        if (wrongMessageFormat){
            return MessageValidationResult.WRONG_MESSAGE_FORMAT;
        }

        bool usernameTooLong = username.Length > ServerConstants.MAX_USERNAME_LEN;
        bool usernameTooShort = username.Length < ServerConstants.MIN_USERNAME_LEN;

        bool passwordTooShort = password.Length < ServerConstants.MIN_PASSWORD_LEN;
        bool passwordTooLong = password.Length > ServerConstants.MAX_PASSWORD_LEN;


        if (usernameTooLong){
            return MessageValidationResult.USERNAME_TOO_LONG;
        }
        else if (usernameTooShort){
            return MessageValidationResult.USERNAME_TOO_SHORT;
        }
        else if (passwordTooShort){
            return MessageValidationResult.PASSWORD_TOO_SHORT;
        }
        else if (passwordTooLong){
            return MessageValidationResult.PASSWORD_TOO_LONG;
        }
        return MessageValidationResult.OK;
    }

    public static void ValidateFileUpload(){

    }
}

public enum MessageValidationResult
{
    OK,
    USERNAME_TOO_LONG,
    USERNAME_TOO_SHORT,
    PASSWORD_TOO_LONG,
    PASSWORD_TOO_SHORT,
    WRONG_MESSAGE_FORMAT,

}
