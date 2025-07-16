using Assets;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.SignalR;

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
                        .AddMessagePackProtocol(options => options.SerializerOptions = new MessagePackSerializerOptions(
                            CompositeResolver.Create(MyResolver.Instance, StandardResolver.Instance)));
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
