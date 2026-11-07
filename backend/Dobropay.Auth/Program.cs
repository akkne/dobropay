using Dobropay.Auth.Api;
using Dobropay.Auth.Persistence;
using Dobropay.Auth.Services;
using Dobropay.Messaging.Database;
using Dobropay.Messaging.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPostgresDbContext<AuthDbContext>(builder.Configuration);
builder.Services.AddInternalSecurity(builder.Configuration);
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();
app.MapAuthEndpoints();
app.Run();
