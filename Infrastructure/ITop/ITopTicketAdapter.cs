using System.Text.Json;
using Core.DTOs.ITop;
using Core.Interfaces.Services;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.ITop
{
    public class ITopTicketAdapter : IITopTicketAdapter
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public ITopTicketAdapter(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        public async Task<ITopTicketCreateResult> CreateTicketAsync(
            ITopTicketCreateRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!bool.TryParse(_config["ITop:Enabled"], out var enabled) || !enabled)
                return ITopTicketCreateResult.Skipped("iTop integration is disabled.");

            var baseUrl = _config["ITop:BaseUrl"];
            var username = _config["ITop:Username"];
            var password = _config["ITop:Password"];

            if (string.IsNullOrWhiteSpace(baseUrl) ||
                string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password))
            {
                return ITopTicketCreateResult.Skipped("iTop BaseUrl, Username, or Password is missing.");
            }

            var fields = BuildFields(request);
            var payload = new
            {
                operation = "core/create",
                @class = _config["ITop:TicketClass"] ?? "UserRequest",
                comment = $"Created from CitizenConnect complaint {request.RefNumber}",
                fields
            };

            var form = new Dictionary<string, string>
            {
                ["auth_user"] = username,
                ["auth_pwd"] = password,
                ["json_data"] = JsonSerializer.Serialize(payload)
            };

            try
            {
                var endpoint = $"{baseUrl.TrimEnd('/')}/webservices/rest.php?version={_config["ITop:ApiVersion"] ?? "1.3"}";
                using var response = await _httpClient.PostAsync(endpoint, new FormUrlEncodedContent(form), cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                    return ITopTicketCreateResult.Failed($"iTop returned HTTP {(int)response.StatusCode}: {body}");

                return ParseCreateResponse(body);
            }
            catch (Exception ex)
            {
                return ITopTicketCreateResult.Failed(ex.Message);
            }
        }

        private Dictionary<string, object> BuildFields(ITopTicketCreateRequest request)
        {
            var fields = new Dictionary<string, object>
            {
                ["title"] = $"[{request.RefNumber}] {request.Title}",
                ["description"] = BuildDescription(request)
            };

            AddConfiguredField(fields, "org_id", "ITop:OrganizationId");
            AddConfiguredField(fields, "caller_id", "ITop:CallerId");
            AddConfiguredField(fields, "service_id", "ITop:ServiceId");
            AddConfiguredField(fields, "servicesubcategory_id", "ITop:ServiceSubcategoryId");

            return fields;
        }

        private static string BuildDescription(ITopTicketCreateRequest request) =>
            $"Complaint Ref: {request.RefNumber}\n" +
            $"Citizen: {request.CitizenName} ({request.CitizenPhone})\n" +
            $"Category: {request.CategoryName}\n" +
            $"Block: {request.BlockName}\n" +
            $"Priority: {request.Priority}\n\n" +
            request.Description;

        private void AddConfiguredField(Dictionary<string, object> fields, string fieldName, string configKey)
        {
            var value = _config[configKey];
            if (!string.IsNullOrWhiteSpace(value))
                fields[fieldName] = value;
        }

        private static ITopTicketCreateResult ParseCreateResponse(string body)
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            if (root.TryGetProperty("code", out var code) && code.GetInt32() != 0)
            {
                var message = root.TryGetProperty("message", out var msg) ? msg.GetString() : body;
                return ITopTicketCreateResult.Failed(message ?? body);
            }

            if (!root.TryGetProperty("objects", out var objects))
                return ITopTicketCreateResult.Failed($"Unexpected iTop response: {body}");

            foreach (var item in objects.EnumerateObject())
            {
                var ticket = item.Value;
                var ticketId = ticket.TryGetProperty("key", out var key) ? key.GetString() : item.Name;
                string? ticketRef = null;

                if (ticket.TryGetProperty("fields", out var fields) &&
                    fields.TryGetProperty("ref", out var refField))
                {
                    ticketRef = refField.GetString();
                }

                return new ITopTicketCreateResult
                {
                    WasAttempted = true,
                    Success = true,
                    TicketId = ticketId,
                    TicketRef = ticketRef ?? ticketId
                };
            }

            return ITopTicketCreateResult.Failed($"No ticket object returned by iTop: {body}");
        }
    }
}
