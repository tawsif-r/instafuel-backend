using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Instafuel.Api.Exceptions;

namespace Instafuel.Api.Modules.Identity;

public sealed class SslWirelessSmsSender(
    HttpClient httpClient,
    IOptions<SmsOptions> options) : ISmsSender
{
    private readonly SmsOptions _options = options.Value;

    public async Task SendAsync(string bangladeshMsisdn, string message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiToken) || string.IsNullOrWhiteSpace(_options.SenderId))
            throw new ServiceUnavailableException("SMS delivery is not configured.");

        var payload = new
        {
            api_token = _options.ApiToken,
            sid = _options.SenderId,
            msisdn = bangladeshMsisdn,
            sms = message,
            csms_id = Guid.NewGuid().ToString("N")[..20]
        };

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        try
        {
            using var response = await httpClient.PostAsJsonAsync(_options.Endpoint, payload, timeout.Token);
            if (!response.IsSuccessStatusCode)
                throw new ServiceUnavailableException("SMS provider rejected the request.");

            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: timeout.Token);
            var root = document.RootElement;
            var accepted = root.TryGetProperty("status", out var status)
                && string.Equals(status.GetString(), "SUCCESS", StringComparison.OrdinalIgnoreCase)
                && root.TryGetProperty("smsinfo", out var smsInfo)
                && smsInfo.ValueKind == JsonValueKind.Array
                && smsInfo.EnumerateArray().Any(item => item.TryGetProperty("sms_status", out var smsStatus)
                    && string.Equals(smsStatus.GetString(), "SUCCESS", StringComparison.OrdinalIgnoreCase));
            if (!accepted)
                throw new ServiceUnavailableException("SMS provider did not accept the message.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ServiceUnavailableException("SMS provider did not respond in time.");
        }
        catch (HttpRequestException)
        {
            throw new ServiceUnavailableException("SMS provider could not be reached.");
        }
        catch (JsonException)
        {
            throw new ServiceUnavailableException("SMS provider returned an invalid response.");
        }
    }
}
