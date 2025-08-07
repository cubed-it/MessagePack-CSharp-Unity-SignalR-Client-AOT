using Microsoft.AspNetCore.SignalR.Client;
using Nerdbank.MessagePack;
using Nerdbank.MessagePack.SignalR;
using PolyType;

[GenerateShapeFor(typeof(object))]
[GenerateShapeFor(typeof(SimpleDto))]
partial class Witness { }

public class SignalRClient2
{
    private CancellationTokenSource _cts;
    private TaskCompletionSource<bool> _tcs;

    public void Init()
    {
        try
        {
            var dto = new SimpleDto() { Value = "Test" };
            MessagePackSerializer serializer = new ();
            byte[] msgpack = serializer.Serialize(dto, Witness.ShapeProvider);
            var deserialized = serializer.Deserialize<SimpleDto>(msgpack, Witness.ShapeProvider);
            if (dto.Value == "Test")
                Log("Nerdbank.MessagePack serialization/deserialization test passed.");
            else
                LogError("Nerdbank.MessagePack serialization/deserialization test failed.");
        }
        catch (Exception ex)
        {
            LogError($"Nerdbank.MessagePack serialization/deserialization failed: {ex.Message}");
            LogException(ex);
        }
    }

    public async Task SendMessage(string host)
    {
        await CancelSendMessage();

        var hubUrl = $"http://{host}/myhub";

        var connection = CreateHubConnection(hubUrl);
        try
        {
            Log($"Attempting to connect to SignalR Hub at: {hubUrl}");
            await connection.StartAsync(_cts.Token);
            Log("Connection started successfully.");

            var message = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            try
            {
                await connection.InvokeAsync("SendMessage", new SimpleDto() { Value = message }, _cts.Token);
                Log($"Message '{message}' sent to server.");
            }
            catch (Exception ex)
            {
                LogError($"Error sending message: {ex.Message}");
                LogException(ex);
            }
        }
        catch (Exception ex)
        {
            LogError($"Error connecting to SignalR Hub: {ex.Message}");
            LogError("Please ensure the SignalR host is running and accessible at the specified URL.");
            LogException(ex);
        }
        finally
        {
            if (connection.State != HubConnectionState.Disconnected)
            {
                Log("Stopping connection...");
                await connection.StopAsync();
                Log("Connection stopped.");
            }

            _tcs.TrySetResult(true);
        }
    }

    private HubConnection CreateHubConnection(string hubUrl)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .AddMessagePackProtocol(Witness.ShapeProvider)
            .Build();

        connection.Closed += (ex) =>
        {
            if(ex != null)
            {
                LogError($"Connection closed with error.");
                LogException(ex);
            }
            else
            {
                Log("Connection closed without error.");
            }
            return Task.CompletedTask;
        };

        connection.Reconnecting += (ex) =>
        {
            LogError($"Connection reconnecting: {ex?.Message}");
            LogException(ex);
            return Task.CompletedTask;
        };

        connection.Reconnected += (connectionId) =>
        {
            Log($"Connection reconnected. New connection ID: {connectionId}");
            return Task.CompletedTask;
        };

        return connection;
    }

    private async Task CancelSendMessage()
    {
        if (_cts != null)
        {
            Log("Cancelling previous send operation...");
            _cts.Cancel();
            await _tcs.Task;
        }

        _cts = new CancellationTokenSource();
        _tcs = new TaskCompletionSource<bool>();
    }

    private string LogTimePrefix => $"{DateTime.Now:HH:mm:ss} ";

    private void Log(string message)
    {
        Console.WriteLine($"{LogTimePrefix}{message}\n");
    }

    private void LogException(Exception ex)
    {
        Console.WriteLine($"{LogTimePrefix}{ex}\n");
    }

    private void LogError(string message)
    {
        var previousColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"{LogTimePrefix}Error: {message}\n");
        Console.ForegroundColor = previousColor;
    }
}
