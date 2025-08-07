using Microsoft.AspNetCore.SignalR;
using Nerdbank.MessagePack.SignalR;
using PolyType;

[GenerateShapeFor(typeof(SimpleDto))]
partial class Witness { }

public class MyHub : Hub
{
    public void SendMessage(SimpleDto dto)
    {
        Console.WriteLine($"Received message from client: {dto.Value}");
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.ConfigureServices(services =>
                {
                    services.AddSignalR()
                        .AddMessagePackProtocol(Witness.ShapeProvider);
                })
                .UseUrls("http://*:5005")
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapHub<MyHub>("/myhub");
                    });
                });
            })
            .Build()
            .Run();
    }
}
