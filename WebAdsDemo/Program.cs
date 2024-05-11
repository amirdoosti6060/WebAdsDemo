using WebAdsDemo.Extensions;
using WebAdsDemo.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IAdsService>(sp =>
{
    string amsNetId = builder.Configuration["TwinCAT:AmsNetId"];
    int port = builder.Configuration.GetValue<int>("TwinCAT:Port");

    return new AdsService(amsNetId, port);
});

var app = builder.Build();

app.ConfigErrorHandler();
app.UseStaticFiles();

app.UseWebSockets();
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/")
    {
        var webSocketService = new WebSocketService();
        await webSocketService.HandleWebSocket(context);
    }
    else
    {
        await next();
    }
});

// Configure the HTTP request pipeline.

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
