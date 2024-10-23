using Antelcat.AspNetCore.ProtooSharp;
using Antelcat.AspNetCore.ProtooSharp.Transports;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddSingleton<WebSocketServer>();
builder.Services.AddTransient<Room>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(60),
});
app.Map("/", async context =>
{
    if (!context.WebSockets.IsWebSocketRequest) throw new InvalidOperationException();

    await context.RequestServices.GetRequiredService<WebSocketServer>().OnRequest(context);
});

app.Services.GetRequiredService<WebSocketServer>().ConnectionRequest += async (info, accept, reject) =>
{
    string? peerId = info.Request.Query["peerId"];

    var logger = app.Services.GetRequiredService<ILogger<Peer>>();
    switch (peerId)
    {
        case null or "reject":
        {
            await reject(403, "Sorry!");
            break;
        }
        default:
        {
            var transport  = await accept();
            var serverPeer = new Peer(peerId, transport!, logger);
            serverPeer.Request += async handler =>
            {
                var method = handler.Request.Request.Method;
                Console.WriteLine(method);
                var data   = handler.Request.WithData<Dictionary<string,string>>();
                Console.WriteLine(data);
                handler.Accept(new { Text = "hi!" });
            };
            break;
        }
    }
};
app.Run();

