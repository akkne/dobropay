using Dobropay.DeveloperApi.Api;
using Dobropay.DeveloperApi.Persistence;
using Dobropay.Messaging.Database;
using Dobropay.Messaging.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPostgresDbContext<MerchantDbContext>(builder.Configuration);
builder.Services.AddInternalSecurity(builder.Configuration);
builder.Services.AddScoped<IMerchantRepository, MerchantRepository>();

var app = builder.Build();
app.MapDeveloperApiEndpoints();
app.Run();
