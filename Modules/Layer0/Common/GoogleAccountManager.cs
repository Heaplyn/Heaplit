// Developer: heaplyn
// Date: 2026-09-01
// Summary: Multi-account store for connected Google accounts. Add several accounts, switch the
//          active one, remove them. The ACTIVE account's tokens are mirrored into the legacy
//          GOOGLE_OAUTH_* settings so all existing code (Gmail, Gemini-via-OAuth, GCloud) keeps
//          working unchanged. Persisted as JSON in GOOGLE_ACCOUNTS_JSON.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace HeaplitLauncher
{
    public class GoogleAccount
    {
        public string Email { get; set; } = "";
        public string AccessToken { get; set; } = "";
        public string RefreshToken { get; set; } = "";
        public DateTime AddedUtc { get; set; } = DateTime.UtcNow;
    }

    public static class GoogleAccountManager
    {
        private static List<GoogleAccount>? _accounts;

        private static List<GoogleAccount> Accounts
        {
            get
            {
                if (_accounts == null) EnsureLoaded();
                return _accounts!;
            }
        }

        public static IReadOnlyList<GoogleAccount> All => Accounts;
        public static string ActiveEmail => CoreRegistry.Data.Settings.Current.GOOGLE_OAUTH_USER_EMAIL;

        private static void EnsureLoaded()
        {
            var s = CoreRegistry.Data.Settings.Current;
            try
            {
                _accounts = string.IsNullOrWhiteSpace(s.GOOGLE_ACCOUNTS_JSON)
                    ? new List<GoogleAccount>()
                    : JsonSerializer.Deserialize<List<GoogleAccount>>(s.GOOGLE_ACCOUNTS_JSON) ?? new List<GoogleAccount>();
            }
            catch { _accounts = new List<GoogleAccount>(); }

            // Seed from legacy single-account fields if the list is empty but a login exists.
            if (_accounts.Count == 0 && !string.IsNullOrWhiteSpace(s.GOOGLE_OAUTH_USER_EMAIL) &&
                !string.IsNullOrWhiteSpace(s.GOOGLE_OAUTH_ACCESS_TOKEN))
            {
                _accounts.Add(new GoogleAccount
                {
                    Email = s.GOOGLE_OAUTH_USER_EMAIL,
                    AccessToken = s.GOOGLE_OAUTH_ACCESS_TOKEN,
                    RefreshToken = s.GOOGLE_OAUTH_REFRESH_TOKEN
                });
                Persist();
            }
        }

        /// <summary>Add or update an account (by email) and make it active.</summary>
        public static void UpsertAndActivate(GoogleAccount acc)
        {
            if (string.IsNullOrWhiteSpace(acc.Email)) return;
            var existing = Accounts.FirstOrDefault(a => a.Email.Equals(acc.Email, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                existing.AccessToken = acc.AccessToken;
                if (!string.IsNullOrWhiteSpace(acc.RefreshToken)) existing.RefreshToken = acc.RefreshToken;
            }
            else Accounts.Add(acc);

            Activate(acc.Email);
            Persist();
        }

        /// <summary>Switch the active account; mirrors its tokens into the legacy settings fields.</summary>
        public static bool Activate(string email)
        {
            var a = Accounts.FirstOrDefault(x => x.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            if (a == null) return false;
            var s = CoreRegistry.Data.Settings.Current;
            s.GOOGLE_OAUTH_USER_EMAIL = a.Email;
            s.GOOGLE_OAUTH_ACCESS_TOKEN = a.AccessToken;
            s.GOOGLE_OAUTH_REFRESH_TOKEN = a.RefreshToken;
            CoreRegistry.Data.Settings.Save();
            return true;
        }

        /// <summary>Called after a token refresh to keep the stored account current.</summary>
        public static void UpdateActiveTokens(string accessToken, string? refreshToken = null)
        {
            var a = Accounts.FirstOrDefault(x => x.Email.Equals(ActiveEmail, StringComparison.OrdinalIgnoreCase));
            if (a == null) return;
            a.AccessToken = accessToken;
            if (!string.IsNullOrWhiteSpace(refreshToken)) a.RefreshToken = refreshToken!;
            Persist();
        }

        public static void Remove(string email)
        {
            Accounts.RemoveAll(a => a.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            if (ActiveEmail.Equals(email, StringComparison.OrdinalIgnoreCase))
            {
                if (Accounts.Count > 0) Activate(Accounts[0].Email);
                else OAuth2Manager.SignOutGoogle();
            }
            Persist();
        }

        private static void Persist()
        {
            try
            {
                CoreRegistry.Data.Settings.Current.GOOGLE_ACCOUNTS_JSON = JsonSerializer.Serialize(_accounts);
                CoreRegistry.Data.Settings.Save();
            }
            catch { }
        }
    }
}
