using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using System.Linq;
using Linux.Bluetooth;
using Linux.Bluetooth.Extensions;

using System.Text.Json;
public class DeviceState {
    public String? addr  { get; set; }
    public bool? connected { get; set; }
}


class Program
{
    private static readonly int port = 15001;
    private static Adapter? adapter = null;
    
    static async Task Main(string[] args)
    {
        Task backgroundTask = Task.Run(() => BackgroundWorker());

        HttpListener listener = new();
        listener.Prefixes.Add($"http://localhost:{port}/");
        listener.Start();
        Console.WriteLine($"Listening for requests on http://localhost:{port}/");

        while (true) {
            HttpListenerContext context = await listener.GetContextAsync();
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;
            if (request.HttpMethod == "GET") {
                await HandleGET(response);
            } else if (request.HttpMethod == "POST") {
                await HandlePOST(request, response);
            } else {
                response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
            }
            response.Close();
        }
    }

    // curl -v localhost:15001
    private static async Task HandleGET(HttpListenerResponse response)
    {
        if (adapter == null) {
            response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
            return ;
        }

        string responseString = "";

        var devices = await adapter.GetDevicesAsync();
        foreach (var device in devices) {
            var deviceProperties = await device.GetAllAsync();
            responseString += $"addr={deviceProperties.Address}, connected={deviceProperties.Connected}, {deviceProperties.Name}\n";
        }

        byte[] buffer = Encoding.UTF8.GetBytes(responseString);
        response.ContentLength64 = buffer.Length;
        response.StatusCode = (int)HttpStatusCode.OK;
        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
    }

    private static async Task HandlePOST(HttpListenerRequest request, HttpListenerResponse response)
    {
        if (adapter == null) {
            response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
            return ;
        }

        using var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding);
        string requestBody = await reader.ReadToEndAsync();
        DeviceState? deviceState = JsonSerializer.Deserialize<DeviceState>(requestBody);
        if (deviceState == null || deviceState.addr == null || deviceState.connected == null) {
            response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
            return ;
        }

        Console.WriteLine($"JSON parsed {deviceState.addr} {deviceState.connected}");

        Device? device = await adapter.GetDeviceAsync(deviceState.addr);
        if (device == null) {
            response.StatusCode = (int)HttpStatusCode.NotFound;
            return;
        }
        var deviceProperties = await device.GetAllAsync();
        if (deviceProperties.Connected != deviceState.connected) {
            if (deviceState.connected == true) {
                await device.ConnectAsync();
            } else {
                await device.DisconnectAsync();
            }
        }
        deviceProperties = await device.GetAllAsync();
        if (deviceProperties.Connected == deviceState.connected) {
            response.StatusCode = (int)HttpStatusCode.OK;
        } else {
            response.StatusCode = (int)HttpStatusCode.Unauthorized;
        }
    }

    private static async Task BackgroundWorker()
    {
        var adapters = await BlueZManager.GetAdaptersAsync();
        if (adapters.Count == 0) {
            Console.WriteLine("No Bluetooth adapters found.");
            return;
        }
        adapter = adapters[0];

        while (true) {
            await adapter.StartDiscoveryAsync();
            await Task.Delay(TimeSpan.FromSeconds(5));
            await adapter.StopDiscoveryAsync();
        }
    }
}
