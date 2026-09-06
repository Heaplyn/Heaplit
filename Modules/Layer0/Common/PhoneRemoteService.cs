// Developer: heaplyn
// Date: 2026-08-16
// Summary: PC-to-Phone remote control client.
// Sends HTTP requests to the mobile companion's background server (port 9001).

using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public static class PhoneRemoteService
    {
        private static readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        private const int PhonePort = 9001;

        public static async Task<string> SendPhoneAction(string phoneIp, string action, string data = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(phoneIp)) return "Error: Phone IP not found.";

                string url = $"http://{phoneIp}:{PhonePort}/api/{action}";
                if (!string.IsNullOrEmpty(data)) url += $"?data={Uri.EscapeDataString(data)}";

                var response = await _http.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                return $"Error: Phone returned {response.StatusCode}";
            }
            catch (Exception ex)
            {
                return $"Connection Error: {ex.Message}";
            }
        }

        public static async Task VibrateAsync(string phoneIp) => await SendPhoneAction(phoneIp, "vibrate");
        public static async Task ToggleFlashlightAsync(string phoneIp) => await SendPhoneAction(phoneIp, "flashlight");
        public static async Task ShowToastAsync(string phoneIp, string message) => await SendPhoneAction(phoneIp, "toast", message);
    }
}
