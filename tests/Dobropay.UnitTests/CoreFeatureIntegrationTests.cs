using Dobropay.Auth.Api;
using Dobropay.Auth.Models;
using Dobropay.Auth.Services;
using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Security;
using Dobropay.DeveloperApi.Api;
using Dobropay.DeveloperApi.Models;
using Dobropay.DeveloperApi.Persistence;
using Dobropay.DeveloperApi.Services;
using Dobropay.Payments.Domain;
using Dobropay.Payments.Persistence;
using Dobropay.Payments.Services;
using Dobropay.UnitTests.Auth;
using Dobropay.UnitTests.DeveloperApi;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Dobropay.UnitTests;

public sealed class CoreFeatureIntegrationTests
{
    private const string InternalKey = "integration-test-key";

    [Fact]
    public async Task RegistrationThenLoginReturnsUsableSession()
    {
        var repository = new FakeAuthRepository();
        var service = new AuthService(repository, new FakeUnitOfWork(), new Sha256HashProvider());
        var registration = await AuthEndpoints.HandleRegisterAsync(
            new RegisterRequest("buyer@example.test", "StrongSecretPassword123!"), service, CancellationToken.None);
        var login = await AuthEndpoints.HandleLoginAsync(
            new LoginRequest("buyer@example.test", "StrongSecretPassword123!"), service, CancellationToken.None);

        Assert.IsType<Ok<AuthResponse>>(registration);
        var loggedIn = Assert.IsType<Ok<AuthResponse>>(login);
        Assert.NotNull(loggedIn.Value?.AccessToken);
        Assert.NotEmpty(repository.Accounts);
    }

    [Fact]
    public async Task RegisteredSessionCanBeResolvedByInternalService()
    {
        var repository = new FakeAuthRepository();
        var hashProvider = new Sha256HashProvider();
        var service = new AuthService(repository, new FakeUnitOfWork(), hashProvider);
        var keyValidator = new InternalKeyValidator(InternalKey, hashProvider);
        var registration = await service.RegisterAsync(
            new RegisterRequest("resolve@example.test", "StrongSecretPassword123!"), CancellationToken.None);
        var context = new DefaultHttpContext();
        context.Request.Headers[HttpHeaderNames.InternalKey] = InternalKey;

        var result = await AuthEndpoints.HandleResolveSessionAsync(
            context, new ResolveSessionRequest(registration.Response!.AccessToken), keyValidator, service, CancellationToken.None);

        var resolved = Assert.IsType<Ok<ResolveSessionResponse>>(result);
        Assert.Equal(registration.Response.AccountId, resolved.Value!.AccountId);
    }

    [Fact]
    public async Task RefreshRotatesTokenAndLogoutRevokesNewSession()
    {
        var service = new AuthService(new FakeAuthRepository(), new FakeUnitOfWork(), new Sha256HashProvider());
        var registration = await service.RegisterAsync(
            new RegisterRequest("rotate@example.test", "StrongSecretPassword123!"), CancellationToken.None);
        var refresh = await AuthEndpoints.HandleRefreshAsync(
            new RefreshRequest(registration.Response!.RefreshToken), service, CancellationToken.None);
        var refreshed = Assert.IsType<Ok<AuthResponse>>(refresh);
        Assert.NotEqual(registration.Response.RefreshToken, refreshed.Value!.RefreshToken);

        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = $"Bearer {refreshed.Value.AccessToken}";
        Assert.IsType<Ok>(await AuthEndpoints.HandleLogoutAsync(context, service, CancellationToken.None));
    }

    [Fact]
    public async Task DuplicateRegistrationIsRejectedAfterFirstAccountIsPersisted()
    {
        var repository = new FakeAuthRepository();
        var service = new AuthService(repository, new FakeUnitOfWork(), new Sha256HashProvider());
        var request = new RegisterRequest("duplicate@example.test", "StrongSecretPassword123!");
        Assert.IsType<Ok<AuthResponse>>(await AuthEndpoints.HandleRegisterAsync(request, service, CancellationToken.None));

        var duplicate = await AuthEndpoints.HandleRegisterAsync(request, service, CancellationToken.None);

        Assert.IsType<Conflict<AuthErrorResponse>>(duplicate);
        Assert.Single(repository.Accounts);
    }

    [Fact]
    public async Task AuthenticatedDeveloperCanCreateShop()
    {
        var sessionResolver = new FakeAuthSessionResolver();
        var accountId = Guid.NewGuid();
        const string token = "shop-create-session";
        sessionResolver.SetSession(token, accountId);
        var repository = new FakeMerchantRepository();
        var service = CreateDeveloperService(repository);
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = $"Bearer {token}";

        var result = await DeveloperApiEndpoints.HandleCreateShopAsync(
            context, new CreateShopRequest("Integration Shop"), sessionResolver, service, CancellationToken.None);

        var created = Assert.IsType<Created<CreateShopResponse>>(result);
        Assert.Equal("Integration Shop", created.Value!.Name);
        Assert.Single(repository.Shops);
    }

    [Fact]
    public async Task MissingDeveloperSessionCannotCreateShop()
    {
        var result = await DeveloperApiEndpoints.HandleCreateShopAsync(
            new DefaultHttpContext(), new CreateShopRequest("Blocked Shop"), new FakeAuthSessionResolver(),
            CreateDeveloperService(new FakeMerchantRepository()), CancellationToken.None);

        Assert.IsType<UnauthorizedHttpResult>(result);
    }

    [Fact]
    public async Task OnboardingApprovalMovesShopIntoApprovedState()
    {
        var repository = new FakeMerchantRepository();
        var shopId = Guid.NewGuid();
        repository.OnboardingProfiles.Add(new ShopOnboardingProfile
        {
            ShopId = shopId,
            VerificationStatus = VerificationStatuses.UnderReview
        });
        var hashProvider = new Sha256HashProvider();
        var context = new DefaultHttpContext();
        context.Request.Headers[HttpHeaderNames.InternalKey] = InternalKey;

        var result = await DeveloperApiEndpoints.HandleApproveOnboardingAsync(
            context, shopId, new InternalKeyValidator(InternalKey, hashProvider),
            CreateDeveloperService(repository), CancellationToken.None);

        var approved = Assert.IsType<Ok<ShopOnboardingDto>>(result);
        Assert.Equal(VerificationStatuses.Approved, approved.Value!.VerificationStatus);
    }

    [Fact]
    public async Task InvalidInternalCredentialCannotApproveOnboarding()
    {
        var repository = new FakeMerchantRepository();
        var hashProvider = new Sha256HashProvider();
        var context = new DefaultHttpContext();
        context.Request.Headers[HttpHeaderNames.InternalKey] = "invalid";

        var result = await DeveloperApiEndpoints.HandleApproveOnboardingAsync(
            context, Guid.NewGuid(), new InternalKeyValidator(InternalKey, hashProvider),
            CreateDeveloperService(repository), CancellationToken.None);

        Assert.IsType<UnauthorizedHttpResult>(result);
    }

    [Fact]
    public async Task FullCaptureUpdatesPaymentAndPublishesOutboxEvent()
    {
        var repository = new FakePaymentsRepository();
        var payment = AddWaitingPayment(repository, 10000);
        var service = new PaymentCaptureService(repository, new FakeUnitOfWork(), new Sha256HashProvider());

        var result = await service.CaptureAsync(payment.ShopId, payment.Id, "capture-full", new CapturePaymentRequest(null),
            "integration-full", CancellationToken.None);

        Assert.Equal(PaymentOperationOutcome.Success, result.Outcome);
        Assert.Equal(PaymentRules.Succeeded, payment.Status);
        Assert.Equal(10000, payment.CapturedAmount);
        Assert.Single(repository.OutboxEvents);
    }

    [Fact]
    public async Task PartialCaptureStoresCapturedAmountAndPublishesEvent()
    {
        var repository = new FakePaymentsRepository();
        var payment = AddWaitingPayment(repository, 10000);
        var service = new PaymentCaptureService(repository, new FakeUnitOfWork(), new Sha256HashProvider());

        var result = await service.CaptureAsync(payment.ShopId, payment.Id, "capture-partial", new CapturePaymentRequest(4500),
            "integration-partial", CancellationToken.None);

        Assert.Equal(PaymentOperationOutcome.Success, result.Outcome);
        Assert.Equal(4500, payment.CapturedAmount);
        Assert.Single(repository.OutboxEvents);
    }

    [Fact]
    public async Task CaptureReplayWithSameKeyDoesNotPublishDuplicateEvent()
    {
        var repository = new FakePaymentsRepository();
        var payment = AddWaitingPayment(repository, 10000);
        var service = new PaymentCaptureService(repository, new FakeUnitOfWork(), new Sha256HashProvider());
        var request = new CapturePaymentRequest(6000);
        var first = await service.CaptureAsync(payment.ShopId, payment.Id, "capture-replay", request, "first", CancellationToken.None);
        var replay = await service.CaptureAsync(payment.ShopId, payment.Id, "capture-replay", request, "replay", CancellationToken.None);

        Assert.Equal(PaymentOperationOutcome.Success, first.Outcome);
        Assert.Equal(PaymentOperationOutcome.Success, replay.Outcome);
        Assert.Single(repository.OutboxEvents);
    }

    [Fact]
    public async Task MerchantCancellationTransitionsPaymentAndPublishesEvent()
    {
        var repository = new FakePaymentsRepository();
        var payment = AddWaitingPayment(repository, 8000);
        var service = new PaymentCaptureService(repository, new FakeUnitOfWork(), new Sha256HashProvider());

        var result = await service.CancelAsync(payment.ShopId, payment.Id, "cancel-hold",
            new CancelPaymentRequest("buyer_declined"), "integration-cancel", CancellationToken.None);

        Assert.Equal(PaymentOperationOutcome.Success, result.Outcome);
        Assert.Equal(PaymentRules.Canceled, payment.Status);
        Assert.Equal(PaymentCancellationParties.Merchant, payment.CancellationParty);
        Assert.Single(repository.OutboxEvents);
    }

    private static DeveloperService CreateDeveloperService(FakeMerchantRepository repository)
    {
        return new DeveloperService(repository, new Sha256HashProvider(), new DeveloperApiOptions
        {
            AuthBaseUrl = "http://auth:8080",
            InternalServiceKey = InternalKey,
            SandboxEnabled = true
        });
    }

    private static Payment AddWaitingPayment(FakePaymentsRepository repository, long amount)
    {
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            ShopId = Guid.NewGuid(),
            Amount = amount,
            Currency = DomainConstants.DefaultCurrency,
            Status = PaymentRules.WaitingForCapture,
            Capture = false
        };
        repository.Payments.Add(payment);
        return payment;
    }
}
