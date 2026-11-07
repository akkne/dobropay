namespace Dobropay.DeveloperApi.Persistence;

public static class MerchantSchemaConstants
{
    public const string ShopsTable = "shops";
    public const string ApiCredentialsTable = "api_credentials";
    public const string ShopMembershipsTable = "shop_memberships";
    public const string IdColumn = "id";
    public const string NameColumn = "name";
    public const string ShopIdColumn = "shop_id";
    public const string KeyHashColumn = "key_hash";
    public const string AccountIdColumn = "account_id";
    public const string RoleColumn = "role";
    public const int MaxShopNameLength = 200;
    public const int KeyHashLength = 64;
    public const int MaxRoleLength = 32;
}
