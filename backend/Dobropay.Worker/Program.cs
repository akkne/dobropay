var builder = Host.CreateApplicationBuilder(args);
_ = builder.Configuration["ConnectionStrings:Worker"] ?? throw new InvalidOperationException("Missing worker connection");
builder.Services.AddHostedService<Worker>();
builder.Services.AddHttpClient();

var host = builder.Build();
host.Run();
