export class DomainConstants {
  public static readonly DefaultCurrency = 'RUB';
  public static readonly CurrencySymbol = '₽';
  public static readonly CheckoutLifetimeMinutes = 30;
  public static readonly DefaultGatewayUrl = 'http://localhost:8080';

  public static readonly StorageKeys = {
    AuthToken: 'dp_auth_token',
    AccountId: 'dp_account_id',
    ApiBaseUrl: 'dp_api_base_url',
    UseMocks: 'dp_use_mocks'
  };
}
