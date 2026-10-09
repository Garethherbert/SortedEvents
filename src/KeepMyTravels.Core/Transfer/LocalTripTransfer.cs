using System.Net;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace KeepMyTravels.Core.Transfer;

/// <summary>A short-lived, single-download TLS session. The QR pins the ephemeral certificate.</summary>
public sealed class LocalTripTransfer : IAsyncDisposable
{
    private readonly TcpListener _listener;
    private readonly X509Certificate2 _certificate;
    private readonly CancellationTokenSource _lifetime = new(TimeSpan.FromMinutes(15));
    private readonly FileStream _file;
    private readonly string _token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    private readonly Task _server;
    public string Link { get; }
    public Task Completion => _server;
    public bool DownloadCompleted { get; private set; }

    private LocalTripTransfer(FileStream file, IPAddress address, string hash)
    {
        _file = file;
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=KeepMyTravels local transfer", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var generated = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(1));
        _certificate = X509CertificateLoader.LoadPkcs12(generated.Export(X509ContentType.Pfx), null);
        _listener = new TcpListener(address, 0);
        _listener.Start(4);
        var port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        var pin = Convert.ToHexString(SHA256.HashData(_certificate.RawData));
        Link = $"keepmytravels://transfer?host={address}&port={port}&token={_token}&pin={pin}&hash={hash}";
        _server = ServeAsync();
    }

    public static async Task<LocalTripTransfer> StartAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var address = NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .SelectMany(n => n.GetIPProperties().UnicastAddresses)
            .Select(n => n.Address).FirstOrDefault(IsPrivateAddress)
            ?? throw new InvalidOperationException("Connect to Wi-Fi or your home network to send a trip by QR code.");
        var file = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, true);
        try
        {
            var hash = Convert.ToHexString(await SHA256.HashDataAsync(file, cancellationToken));
            file.Position = 0;
            return new LocalTripTransfer(file, address, hash);
        }
        catch { await file.DisposeAsync(); throw; }
    }

    private async Task ServeAsync()
    {
        try
        {
            while (!_lifetime.IsCancellationRequested)
            {
                using var client = await _listener.AcceptTcpClientAsync(_lifetime.Token);
                using var handshake = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
                handshake.CancelAfter(TimeSpan.FromSeconds(10));
                await using var tls = new SslStream(client.GetStream(), false);
                try
                {
                    await tls.AuthenticateAsServerAsync(new SslServerAuthenticationOptions
                    {
                        ServerCertificate = _certificate,
                        EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
                    }, handshake.Token);
                    var header = new StringBuilder();
                    var b = new byte[1];
                    while (header.Length < 4096 && !header.ToString().EndsWith("\r\n\r\n", StringComparison.Ordinal))
                    {
                        if (await tls.ReadAsync(b, handshake.Token) == 0) break;
                        header.Append((char)b[0]);
                    }
                    var firstLine = header.ToString().Split("\r\n", 2)[0];
                    if (firstLine != $"GET /{_token} HTTP/1.1" || !header.ToString().EndsWith("\r\n\r\n", StringComparison.Ordinal))
                    {
                        await tls.WriteAsync(Encoding.ASCII.GetBytes("HTTP/1.1 404 Not Found\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"), handshake.Token);
                        continue;
                    }
                    var response = $"HTTP/1.1 200 OK\r\nContent-Type: application/vnd.keepmytravels.voyage\r\nContent-Disposition: attachment; filename=trip.voyage\r\nContent-Length: {_file.Length}\r\nCache-Control: no-store\r\nConnection: close\r\n\r\n";
                    await tls.WriteAsync(Encoding.ASCII.GetBytes(response), _lifetime.Token);
                    _file.Position = 0;
                    await _file.CopyToAsync(tls, _lifetime.Token);
                    await tls.FlushAsync(_lifetime.Token);
                    DownloadCompleted = true;
                    return;
                }
                catch (Exception ex) when (ex is IOException or AuthenticationException or OperationCanceledException)
                {
                    // Bad requests and interrupted downloads do not consume the transfer.
                }
            }
        }
        catch (OperationCanceledException) { }
        finally { _listener.Stop(); }
    }

    public static LocalTripTransferLink ParseLink(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != "keepmytravels" || uri.Host != "transfer" ||
            uri.AbsolutePath is not ("" or "/") || uri.UserInfo.Length != 0 || uri.Fragment.Length != 0 || uri.Port != -1)
            throw new InvalidDataException("Scan a KeepMyTravels transfer QR code.");
        Dictionary<string, string> parts;
        try
        {
            parts = uri.Query.TrimStart('?').Split('&').Select(x => x.Split('=', 2))
                .ToDictionary(x => x[0], x => Uri.UnescapeDataString(x[1]), StringComparer.Ordinal);
        }
        catch (Exception ex) when (ex is ArgumentException or IndexOutOfRangeException or UriFormatException)
        { throw new InvalidDataException("The transfer QR code is invalid.", ex); }
        if (parts.Count != 5 || !parts.TryGetValue("host", out var host) || !IPAddress.TryParse(host, out var address) || !IsPrivateAddress(address) ||
            !parts.TryGetValue("port", out var portText) || !int.TryParse(portText, out var port) || port is < 1024 or > 65535 ||
            !parts.TryGetValue("token", out var token) || !IsDigest(token) ||
            !parts.TryGetValue("pin", out var pin) || !IsDigest(pin) ||
            !parts.TryGetValue("hash", out var hash) || !IsDigest(hash))
            throw new InvalidDataException("The transfer QR code is invalid or does not point to a local device.");
        return new LocalTripTransferLink(new Uri($"https://{address}:{port}/{token}"), pin, hash);
    }

    public static async Task DownloadAsync(string link, string destination, CancellationToken cancellationToken = default)
    {
        var transfer = ParseLink(link);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromMinutes(15));
        cancellationToken = deadline.Token;
        // This client is used only for the private IP address in the scanned QR. Never
        // change the shared application HttpClient's certificate validation policy.
        using var handler = new SocketsHttpHandler { AllowAutoRedirect = false, UseProxy = false, ConnectTimeout = TimeSpan.FromSeconds(15) };
        handler.SslOptions.RemoteCertificateValidationCallback = (_, certificate, _, _) =>
            certificate is not null && CryptographicOperations.FixedTimeEquals(
                SHA256.HashData(certificate.GetRawCertData()), Convert.FromHexString(transfer.CertificatePin));
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(15) };
        try
        {
            using var response = await client.GetAsync(transfer.Url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (response.StatusCode != HttpStatusCode.OK)
                throw new InvalidDataException("This transfer is no longer available. Generate a new QR code on the sending device.");
            const long maxBytes = 20L * 1024 * 1024 * 1024;
            if (response.Content.Headers.ContentLength is not long expected || expected <= 0 || expected > maxBytes)
                throw new InvalidDataException("The transfer has an invalid file size.");
            await using (var source = await response.Content.ReadAsStreamAsync(cancellationToken))
            await using (var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, 64 * 1024, true))
            {
                var buffer = new byte[64 * 1024];
                long total = 0;
                int read;
                while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    total += read;
                    if (total > expected) throw new InvalidDataException("The transfer exceeded its declared size.");
                    await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                }
                if (total != expected) throw new InvalidDataException("The transfer was interrupted. Generate a new QR code and try again.");
            }
            await using var downloaded = File.OpenRead(destination);
            var actual = await SHA256.HashDataAsync(downloaded, cancellationToken);
            if (!CryptographicOperations.FixedTimeEquals(actual, Convert.FromHexString(transfer.FileHash)))
                throw new InvalidDataException("The downloaded trip does not match the QR code. Send the trip again.");
        }
        catch (HttpRequestException ex)
        {
            TryDelete(destination);
            throw new IOException("Could not connect to the sending device. Keep both devices on the same Wi-Fi, leave its QR screen open, and allow KeepMyTravels through Windows Firewall on private networks.", ex);
        }
        catch { TryDelete(destination); throw; }
    }

    private static bool IsDigest(string value) => value.Length == 64 && value.All(Uri.IsHexDigit);
    private static bool IsPrivateAddress(IPAddress address)
    {
        if (address.AddressFamily != AddressFamily.InterNetwork) return false;
        var bytes = address.GetAddressBytes();
        return bytes[0] == 10 || (bytes[0] == 172 && bytes[1] is >= 16 and <= 31) || (bytes[0] == 192 && bytes[1] == 168);
    }
    private static void TryDelete(string path) { try { File.Delete(path); } catch { } }
    public async ValueTask DisposeAsync()
    {
        _lifetime.Cancel();
        _listener.Stop();
        try { await _server; } catch (Exception ex) when (ex is SocketException or ObjectDisposedException or OperationCanceledException) { }
        await _file.DisposeAsync();
        _certificate.Dispose();
        _lifetime.Dispose();
    }
}

public sealed record LocalTripTransferLink(Uri Url, string CertificatePin, string FileHash);
