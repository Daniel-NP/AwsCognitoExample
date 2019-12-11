namespace AwsCognitoExample.Helpers
{
    public enum CognitoResultType
    {
        Unknown,
        Ok,
        PasswordChangeRequired,
        RegisterOk,
        NotAuthorized,
        Error,
        UserNotFound,
        UserNameAlreadyUsed,
        EmailAlreadyUsed,
        PasswordRequirementsFailed,
        NotConfirmed,
        Timeout,
        Offline
    }
}