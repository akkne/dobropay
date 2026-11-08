namespace Dobropay.Auth.Models;

public enum RegisterOutcome
{
    Success,
    AlreadyExists,
    InvalidEmail,
    InvalidPassword
}
