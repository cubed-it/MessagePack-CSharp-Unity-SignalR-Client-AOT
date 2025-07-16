using Assets;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows;

public class SignalRClient : MonoBehaviour
{
    [SerializeField] private InputField _inputField;
    [SerializeField] private Button _sendButton;
    [SerializeField] private Text _logText;

    private CancellationTokenSource _cts;
    private TaskCompletionSource<bool> _tcs;

    private void Start()
    {
        _inputField.text = "192.168.145.50:5005";

        _sendButton.onClick.AddListener(() =>
        {
            if (Uri.IsWellFormedUriString($"http://{_inputField.text}", UriKind.Absolute))
                SendMessage();
        });

        MessagePackSerializer.DefaultOptions = new MessagePackSerializerOptions(
                CompositeResolver.Create(MyResolver.Instance, StandardResolver.Instance));

        try
        {
            var result = MessagePackSerializer.Deserialize<SimpleDto>(MessagePackSerializer.Serialize(
                new SimpleDto() { Value = "Test" }));
            if (result.Value == "Test")
                Log("MessagePack serialization/deserialization test passed.");
            else
                LogError("MessagePack serialization/deserialization test failed.");
        }
        catch (Exception ex)
        {
            LogError($"MessagePack serialization/deserialization failed: {ex.Message}");
            LogException(ex);
        }
    }

    public async void SendMessage()
    {
        await CancelSendMessage();

        _logText.text = string.Empty;

        var hubUrl = $"http://{_inputField.text}/myhub";

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
            .AddMessagePackProtocol()
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

    private string LogTimePrefix => $"<color=#808080>{DateTime.Now:HH:mm:ss}  </color>";

    private void Log(string message)
    {
        _logText.text += $"{LogTimePrefix}{message}\n";
        Debug.Log(message);
    }

    private void LogException(Exception ex)
    {
        //_logText.text += $"{LogTimePrefix}<color=#D92626>Exception: {Regex.Replace(ex.ToString(), @"\p{C}", string.Empty)}</color>\n";
        Debug.LogException(ex);
    }

    private void LogError(string message)
    {
        _logText.text += $"{LogTimePrefix}<color=#D92626>Error: {message}</color>\n";
        Debug.LogError(message);
    }
}
