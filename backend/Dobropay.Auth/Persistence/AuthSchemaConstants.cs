namespace Dobropay.Auth.Persistence;

public static class AuthSchemaConstants
{
    public const string AccountsTable = "accounts";
    public const string StaffSessionsTable = "staff_sessions";
    public const string IdColumn = "id";
    public const string EmailColumn = "email";
    public const string PasswordSaltColumn = "password_salt";
    public const string PasswordHashColumn = "password_hash";
    public const string TokenHashColumn = "token_hash";
    public const string AccountIdColumn = "account_id";
    public const string ExpiresAtColumn = "expires_at";
}
