using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace RiotPicker.Services;

public class BaseConnector
{
    protected HttpClient Client { get; private set; }
    public string? LockfilePath { get; set; }
    public int? Port { get; set; }
    public string? Password { get; set; }
    public string Protocol { get; set; } = "https";
    public bool Connected { get; set; }

    public BaseConnector(string lockfilePath = "")
    {
        LockfilePath = lockfilePath;
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
        Client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };
    }

    public string BaseUrl => $"{Protocol}://127.0.0.1:{Port}";

    public bool ParseLockfile()
    {
        if (string.IsNullOrEmpty(LockfilePath) || !File.Exists(LockfilePath))
        {
            Connected = false;
            return false;
        }
        try
        {
            // Open with FileShare.ReadWrite - LoL client keeps file locked
            using var fs = new FileStream(LockfilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var reader = new StreamReader(fs);
            var content = reader.ReadToEnd().Trim();
            var parts = content.Split(':');
            if (parts.Length < 5)
            {
                Connected = false;
                return false;
            }
            Port = int.Parse(parts[2]);
            Password = parts[3];
            Protocol = parts[4];
            SetBasicAuth("riot", Password);
            Connected = true;
            return true;
        }
        catch
        {
            Connected = false;
            return false;
        }
    }

    protected void SetBasicAuth(string username, string password)
    {
        var encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encoded);
    }

    public async Task<HttpResponseMessage?> RequestAsync(HttpMethod method, string endpoint,
        HttpContent? content = null, Dictionary<string, string>? headers = null)
    {
        if (!Connected) return null;
        try
        {
            var url = $"{BaseUrl}{endpoint}";
            var request = new HttpRequestMessage(method, url);
            if (content != null) request.Content = content;
            if (headers != null)
            {
                foreach (var (key, value) in headers)
                    request.Headers.TryAddWithoutValidation(key, value);
            }
            return await Client.SendAsync(request);
        }
        catch
        {
            return null;
        }
    }

    public Task<HttpResponseMessage?> GetAsync(string endpoint, Dictionary<string, string>? headers = null)
        => RequestAsync(HttpMethod.Get, endpoint, headers: headers);

    public Task<HttpResponseMessage?> PostAsync(string endpoint, HttpContent? content = null,
        Dictionary<string, string>? headers = null)
        => RequestAsync(HttpMethod.Post, endpoint, content, headers);

    public Task<HttpResponseMessage?> PatchAsync(string endpoint, HttpContent? content = null,
        Dictionary<string, string>? headers = null)
        => RequestAsync(HttpMethod.Patch, endpoint, content, headers);

    public bool IsRunning() =>
        !string.IsNullOrEmpty(LockfilePath) && File.Exists(LockfilePath);
}
