public class Program
{
    public static async Task Main(string[] args)
    {
        var client = new SignalRClient2();
        client.Init();
        Console.WriteLine("Press Enter to send a message to the SignalR Hub or type 'exit' to quit.");
        while (Console.ReadLine() != "exit")
        {
            await client.SendMessage(args.FirstOrDefault() ?? "192.168.145.50:5005");
        }
    }
}