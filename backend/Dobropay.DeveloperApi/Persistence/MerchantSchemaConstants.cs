namespace Dobropay.DeveloperApi.Persistence;

public static class MerchantSchemaConstants
{
    public const string ShopsTable = "shops";
    public const string ApiCredentialsTable = "api_credentials";
    public const string ShopMembershipsTable = "shop_memberships";
    public const string ShopOnboardingProfilesTable = "shop_onboarding_profiles";
    public const string IdColumn = "id";
    public const string NameColumn = "name";
    public const string ShopIdColumn = "shop_id";
    public const string KeyHashColumn = "key_hash";
    public const string AccountIdColumn = "account_id";
    public const string RoleColumn = "role";
    public const string InnColumn = "inn";
    public const string OgrnColumn = "ogrn";
    public const string LegalNameColumn = "legal_name";
    public const string BankBicColumn = "bank_bic";
    public const string BankAccountColumn = "bank_account";
    public const string WebsiteUrlColumn = "website_url";
    public const string VerificationStatusColumn = "verification_status";
    public const string CreatedAtColumn = "created_at";
    public const string UpdatedAtColumn = "updated_at";
    public const string ModeColumn = "mode";
    public const string KeyPrefixColumn = "key_prefix";
    public const string RevokedAtColumn = "revoked_at";
    public const int MaxShopNameLength = 200;
    public const int KeyHashLength = 64;
    public const int MaxRoleLength = 32;
    public const int MaxInnLength = 12;
    public const int MaxOgrnLength = 15;
    public const int MaxLegalNameLength = 300;
    public const int MaxBankBicLength = 9;
    public const int MaxBankAccountLength = 30;
    public const int MaxWebsiteUrlLength = 500;
    public const int MaxVerificationStatusLength = 32;
    public const int MaxKeyNameLength = 100;
    public const int MaxKeyModeLength = 16;
    public const int MaxKeyPrefixLength = 32;
}
