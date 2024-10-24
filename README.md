# ProtooSharp
ported to .NET from [protoo](https://github.com/versatica/protoo)

## Usage

in ASP.NET

```csharp
var builder = WebApplication.CreateBuilder();

builder.Services.AddProtooServer<WebSocketServer>(); // add protoo server

var app = builder.Build();

app.UseWebSockets(); // use websockets before map protoo server
app.MapProtooServer<WebSocketServer>("/{your}/{route}"); //map protoo server

app.Run();

```

