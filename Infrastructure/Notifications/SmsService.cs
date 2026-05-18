using Core.Common;
using Core.DTOs.Mobile;
using Core.Interfaces.Services;
using Microsoft.Extensions.Options;

namespace Infrastructure.Notifications
{
    public class SmsService : ISmsService
    {
        private readonly SmsProviderOptions _options;
        private readonly HttpClient _httpClient;

        public SmsService(IOptions<SmsProviderOptions> options, HttpClient httpClient)
        {
            _options = options.Value;
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (ASP.NET Core)");
        }

        public async Task SendAsync(string mobileNumber, string message)
        {
            var mobile = MobileNumberNormalizer.ToSmsProviderMobile(mobileNumber);
            var encodedMessage = Uri.EscapeDataString(message);
            var url =
                $"{_options.BaseUrl}?" +
                $"AUTH_KEY={_options.AuthKey}" +
                $"&message={encodedMessage}" +
                $"&senderId={_options.SenderId}" +
                $"&routeId={_options.RouteId}" +
                $"&mobileNos={mobile}" +
                $"&smsContentType={_options.SmsContentType}";

            var response = await _httpClient.GetAsync(url);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode ||
                responseText.Contains("error", StringComparison.OrdinalIgnoreCase))
            {
                throw new ApplicationException(
                    $"SMS failed. Status: {response.StatusCode}, Response: {responseText}");
            }
        }
    }
}
