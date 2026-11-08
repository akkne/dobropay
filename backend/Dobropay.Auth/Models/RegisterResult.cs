namespace Dobropay.Auth.Models;

public sealed class RegisterResult
{
    public RegisterOutcome Outcome { get; init; }
    public AuthResponse? Response { get; init; }

    public static RegisterResult Success(AuthResponse response) =>
        new() { Outcome = RegisterOutcome.Success, Response = response };

    public static RegisterResult AlreadyExists() =>
        new() { Outcome = RegisterOutcome.AlreadyExists };

    public static RegisterResult InvalidEmail() =>
        new() { Outcome = RegisterOutcome.InvalidEmail };

    public static RegisterResult InvalidPassword() =>
        new() { Outcome = RegisterOutcome.InvalidPassword };
}
