using System.Net;
using System.Net.Http.Headers;

namespace KeepMyTravels.Core.Transfer;

public static class SafeTransferDownload
{
    // The caller must disable automatic redirects on the supplied HttpClient.
    // Every destination is checked before a request, not after downloading it.
    public static async Task<HttpResponseMessage> SendAsync(HttpClient client, Uri uri, string userAgent, CancellationToken cancellationToken = default)
    {
        for (var hop = 0; hop < 6; hop++)
        {
            if (uri.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(uri.UserInfo))
                throw new InvalidDataException("Download links and redirects must use HTTPS without a username or password.");
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.keepmytravels.voyage"));
            request.Headers.UserAgent.ParseAdd(userAgent);
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (response.StatusCode is not (HttpStatusCode.MovedPermanently or HttpStatusCode.Redirect or HttpStatusCode.SeeOther or HttpStatusCode.TemporaryRedirect or HttpStatusCode.PermanentRedirect))
                return response;
            var location = response.Headers.Location;
            response.Dispose();
            if (location is null) throw new InvalidDataException("The download returned a redirect without a destination.");
            uri = location.IsAbsoluteUri ? location : new Uri(uri, location);
        }
        throw new InvalidDataException("The download redirected too many times. Use a direct HTTPS file link.");
    }
}
