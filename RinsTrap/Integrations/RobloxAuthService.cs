using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RinsTrap.Integrations
{
    public static class RobloxAuthService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static async Task<RobloxUserInfo?> ValidateCookieAsync(string cookie)
        {
            if (string.IsNullOrWhiteSpace(cookie))
                return null;

            try
            {
                var handler = new HttpClientHandler
                {
                    CookieContainer = new CookieContainer(),
                    UseCookies = true
                };

                var cookieContainer = new CookieContainer();
                // Add cookie for both roblox.com and users.roblox.com domains
                cookieContainer.Add(new Uri("https://roblox.com"), new Cookie(".ROBLOSECURITY", cookie));
                cookieContainer.Add(new Uri("https://users.roblox.com"), new Cookie(".ROBLOSECURITY", cookie));
                cookieContainer.Add(new Uri("https://www.roblox.com"), new Cookie(".ROBLOSECURITY", cookie));
                handler.CookieContainer = cookieContainer;

                using var client = new HttpClient(handler);
                client.DefaultRequestHeaders.Add("User-Agent", "Roblox/WinInet");

                // Get authenticated user info
                var response = await client.GetAsync("https://users.roblox.com/v1/users/authenticated");
                if (!response.IsSuccessStatusCode)
                {
                    App.Logger.WriteLine("RobloxAuthService", $"Cookie validation failed: {response.StatusCode}");
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var userInfo = JsonSerializer.Deserialize<RobloxUserInfo>(json, JsonOptions);

                return userInfo;
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine("RobloxAuthService", $"Cookie validation error: {ex.Message}");
                return null;
            }
        }

        public static async Task<string?> GetAuthTicketAsync(string cookie)
        {
            if (string.IsNullOrWhiteSpace(cookie))
                return null;

            try
            {
                var handler = new HttpClientHandler
                {
                    UseCookies = false // We'll manually set cookie
                };

                using var client = new HttpClient(handler);
                client.DefaultRequestHeaders.Add("User-Agent", "Roblox/WinInet");
                client.DefaultRequestHeaders.Add("Referer", "https://www.roblox.com");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
                client.DefaultRequestHeaders.Add("Cookie", $".ROBLOSECURITY={cookie}");

                // Get CSRF token from a working endpoint
                string? csrfToken = null;
                
                var endpoints = new[]
                {
                    "https://users.roblox.com/v1/users/authenticated",
                    "https://accountsettings.roblox.com/v1/themes",
                    "https://accountinformation.roblox.com/v1/birthdate",
                    "https://friends.roblox.com/v1/users/1/friends"
                };

                foreach (var endpoint in endpoints)
                {
                    try
                    {
                        var csrfResponse = await client.PostAsync(endpoint, null);
                        App.Logger.WriteLine("RobloxAuthService", $"CSRF attempt {endpoint}: {csrfResponse.StatusCode}");
                        if (csrfResponse.Headers.TryGetValues("x-csrf-token", out var tokens))
                        {
                            csrfToken = tokens.FirstOrDefault();
                            if (!string.IsNullOrEmpty(csrfToken))
                            {
                                App.Logger.WriteLine("RobloxAuthService", $"Got CSRF token from {endpoint}: {csrfToken.Substring(0, Math.Min(20, csrfToken.Length))}...");
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        App.Logger.WriteLine("RobloxAuthService", $"CSRF attempt {endpoint} error: {ex.Message}");
                    }
                }

                if (string.IsNullOrEmpty(csrfToken))
                {
                    App.Logger.WriteLine("RobloxAuthService", "Failed to get CSRF token from all endpoints");
                    return null;
                }

                client.DefaultRequestHeaders.Remove("x-csrf-token");
                client.DefaultRequestHeaders.Add("x-csrf-token", csrfToken);

                // Request authentication ticket
                var ticketResponse = await client.PostAsync("https://auth.roblox.com/v1/authentication-ticket", null);
                App.Logger.WriteLine("RobloxAuthService", $"Ticket request status: {ticketResponse.StatusCode}");
                
                if (!ticketResponse.IsSuccessStatusCode)
                {
                    var errorContent = await ticketResponse.Content.ReadAsStringAsync();
                    App.Logger.WriteLine("RobloxAuthService", $"Ticket request failed: {ticketResponse.StatusCode}, Content: {errorContent}");
                    return null;
                }

                // Check headers first
                if (ticketResponse.Headers.TryGetValues("rbx-authentication-ticket", out var ticketTokens))
                {
                    var ticket = ticketTokens.FirstOrDefault();
                    App.Logger.WriteLine("RobloxAuthService", $"Got ticket from header: {ticket?.Substring(0, Math.Min(20, ticket.Length))}...");
                    return ticket;
                }

                // Check response body
                var responseBody = await ticketResponse.Content.ReadAsStringAsync();
                App.Logger.WriteLine("RobloxAuthService", $"Ticket response body: {responseBody}");
                
                try
                {
                    using var doc = JsonDocument.Parse(responseBody);
                    if (doc.RootElement.TryGetProperty("authenticationTicket", out var ticketElement))
                    {
                        var ticket = ticketElement.GetString();
                        App.Logger.WriteLine("RobloxAuthService", $"Got ticket from body: {ticket?.Substring(0, Math.Min(20, ticket.Length))}...");
                        return ticket;
                    }
                    if (doc.RootElement.TryGetProperty("ticket", out ticketElement))
                    {
                        var ticket = ticketElement.GetString();
                        App.Logger.WriteLine("RobloxAuthService", $"Got ticket from body (ticket): {ticket?.Substring(0, Math.Min(20, ticket.Length))}...");
                        return ticket;
                    }
                }
                catch { }

                App.Logger.WriteLine("RobloxAuthService", "No ticket found in response");
                return null;
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine("RobloxAuthService", $"Auth ticket error: {ex.Message}");
                return null;
            }
        }

        public static async Task<string?> GetUserThumbnailAsync(string userId, string size = "150x150")
        {
            try
            {
                var payload = new
                {
                    userIds = new[] { userId },
                    size = size,
                    format = "Png",
                    isCircular = false
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var json = await App.HttpClient.PostFromJsonWithRetriesAsync<ThumbnailBatchResponse>(
                    "https://thumbnails.roblox.com/v1/batch", content, 3, CancellationToken.None);

                return json?.Data?.FirstOrDefault()?.ImageUrl;
            }
            catch
            {
                return null;
            }
        }
    }

    public class RobloxUserInfo
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; } = "";

        [JsonPropertyName("description")]
        public string Description { get; set; } = "";
    }

    public class ThumbnailBatchResponse
    {
        [JsonPropertyName("data")]
        public List<ThumbnailData>? Data { get; set; }
    }

    public class ThumbnailData
    {
        [JsonPropertyName("targetId")]
        public long TargetId { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; } = "";

        [JsonPropertyName("imageUrl")]
        public string ImageUrl { get; set; } = "";
    }
}