using System.Net.Http.Json;

var builder = WebApplication.CreateBuilder(args);
var authUrl = builder.Configuration["Services:Auth"] ?? throw new InvalidOperationException("Services:Auth missing");
var developerUrl = builder.Configuration["Services:DeveloperApi"] ?? throw new InvalidOperationException("Services:DeveloperApi missing");
var paymentsUrl = builder.Configuration["Services:Payments"] ?? throw new InvalidOperationException("Services:Payments missing");
var notificationsUrl = builder.Configuration["Services:Notifications"] ?? throw new InvalidOperationException("Services:Notifications missing");
var receiptsUrl = builder.Configuration["Services:Receipts"] ?? throw new InvalidOperationException("Services:Receipts missing");
var serviceKey = builder.Configuration["INTERNAL_SERVICE_KEY"] ?? throw new InvalidOperationException("INTERNAL_SERVICE_KEY missing");
builder.Services.AddHttpClient();
var app = builder.Build();
app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready" }));

app.MapGet("/v1/admin/shops/{shopId:guid}/payments", (HttpContext http, Guid shopId, CancellationToken ct) => Forward(http, shopId, paymentsUrl, "payments", ct));
app.MapGet("/v1/admin/shops/{shopId:guid}/payments/{id:guid}", (HttpContext http, Guid shopId, Guid id, CancellationToken ct) =>
    Forward(http, shopId, paymentsUrl, "payments/" + id, ct));
app.MapGet("/v1/admin/shops/{shopId:guid}/deliveries", (HttpContext http, Guid shopId, CancellationToken ct) => Forward(http, shopId, notificationsUrl, "deliveries", ct));
app.MapGet("/v1/admin/shops/{shopId:guid}/receipts", (HttpContext http, Guid shopId, CancellationToken ct) => Forward(http, shopId, receiptsUrl, "receipts", ct));

app.Run();

async Task<IResult> Forward(HttpContext http, Guid shopId, string target, string path, CancellationToken ct)
{
    var bearer = http.Request.Headers.Authorization.ToString();
    if (!bearer.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return Results.Unauthorized();
    var client = app.Services.GetRequiredService<IHttpClientFactory>().CreateClient();
    client.DefaultRequestHeaders.Add("X-Internal-Key", serviceKey);
    using var session = await client.PostAsJsonAsync(authUrl + "/internal/v1/sessions/resolve", new { Token = bearer[7..] }, ct);
    if (!session.IsSuccessStatusCode) return Results.Unauthorized();
    var identity = await session.Content.ReadFromJsonAsync<Identity>(cancellationToken: ct);
    if (identity is null) return Results.Unauthorized();
    using var membership = await client.GetAsync(developerUrl + "/internal/v1/memberships/" + identity.AccountId + "/" + shopId, ct);
    if (!membership.IsSuccessStatusCode) return Results.NotFound();
    using var response = await client.GetAsync(target + "/internal/v1/shops/" + shopId + "/" + path, ct);
    var json = await response.Content.ReadAsStringAsync(ct);
    return Results.Content(json, "application/json", statusCode: (int)response.StatusCode);
}

public sealed record Identity(Guid AccountId);
