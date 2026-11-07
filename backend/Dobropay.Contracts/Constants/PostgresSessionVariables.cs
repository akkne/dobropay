namespace Dobropay.Contracts.Constants;

public static class PostgresSessionVariables
{
    public const string ShopId = "app.shop_id";
    public const string SetShopIdSql = "SELECT set_config('app.shop_id', $1, true)";
}
