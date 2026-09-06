// Developer: heaplyn
// Date: 2026-08-13
// Summary: Handles CLI/HUD commands to open the OAuth2 Studio or trigger Google / GitHub login.

using System;
using System.Collections.Generic;

namespace HeaplitLauncher
{
    public class OAuth2CommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            return SearchUtil.MatchesAny(query, "oauth", "auth", "login", "google login", "github login", "oauth2");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            string lower = query.Trim().ToLower();

            // Option 1: Open OAuth2 Studio
            suggestions.Add(new CommandResult
            {
                TITLE = "🔑 Open OAuth2 Account Authentication Studio",
                DESCRIPTION = "Manage Google Gemini AI & GitHub OAuth2 account logins and tokens",
                SIMILARITY = (SearchUtil.BestSimilarity(query, "oauth", "auth", "login", "google login", "github login", "oauth2") + 6.0 * 0.01),
                EXECUTE = () => OAuth2StudioOverlay.ShowOverlay()
            });

            // Option 2: Direct Google Login
            if (lower.Contains("google"))
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = "🔑 Sign In with Google OAuth2",
                    DESCRIPTION = "Launch browser to authorize Google Gemini AI access token",
                    SIMILARITY = (SearchUtil.BestSimilarity(query, "oauth", "auth", "login", "google login", "github login", "oauth2") + 6.5 * 0.01),
                    EXECUTE = async () => await OAuth2Manager.LoginGoogleOAuth2Async(status => TextOverlay.Show(status, 2500))
                });
            }

            // Option 3: Direct GitHub Login
            if (lower.Contains("github"))
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = "🐙 Sign In with GitHub OAuth2",
                    DESCRIPTION = "Launch browser to authorize GitHub access token",
                    SIMILARITY = (SearchUtil.BestSimilarity(query, "oauth", "auth", "login", "google login", "github login", "oauth2") + 6.5 * 0.01),
                    EXECUTE = async () => await OAuth2Manager.LoginGithubOAuth2Async(status => TextOverlay.Show(status, 2500))
                });
            }

            // Custom Credentials Setter Suggesters
            if (lower.StartsWith("set google_client_id "))
            {
                string val = query.Substring("set google_client_id ".Length).Trim();
                suggestions.Add(new CommandResult
                {
                    TITLE = $"💾 Set Google OAuth2 Client ID to: \"{val}\"",
                    DESCRIPTION = "Saves custom Google Cloud Client ID for Gemini authorization",
                    SIMILARITY = (SearchUtil.BestSimilarity(query, "oauth", "auth", "login", "google login", "github login", "oauth2") + 7.0 * 0.01),
                    EXECUTE = () => { SettingsManager.Current.GOOGLE_OAUTH_CLIENT_ID = val; SettingsManager.Save(); TextOverlay.Show("✅ Google Client ID saved!", 2500); }
                });
            }
            if (lower.StartsWith("set google_client_secret "))
            {
                string val = query.Substring("set google_client_secret ".Length).Trim();
                suggestions.Add(new CommandResult
                {
                    TITLE = $"💾 Set Google OAuth2 Client Secret to: \"{val}\"",
                    DESCRIPTION = "Saves custom Google Cloud Client Secret",
                    SIMILARITY = (SearchUtil.BestSimilarity(query, "oauth", "auth", "login", "google login", "github login", "oauth2") + 7.0 * 0.01),
                    EXECUTE = () => { SettingsManager.Current.GOOGLE_OAUTH_CLIENT_SECRET = val; SettingsManager.Save(); TextOverlay.Show("✅ Google Client Secret saved!", 2500); }
                });
            }
            if (lower.StartsWith("set github_client_id "))
            {
                string val = query.Substring("set github_client_id ".Length).Trim();
                suggestions.Add(new CommandResult
                {
                    TITLE = $"💾 Set GitHub OAuth2 Client ID to: \"{val}\"",
                    DESCRIPTION = "Saves custom GitHub Client ID",
                    SIMILARITY = (SearchUtil.BestSimilarity(query, "oauth", "auth", "login", "google login", "github login", "oauth2") + 7.0 * 0.01),
                    EXECUTE = () => { SettingsManager.Current.GITHUB_OAUTH_CLIENT_ID = val; SettingsManager.Save(); TextOverlay.Show("✅ GitHub Client ID saved!", 2500); }
                });
            }
            if (lower.StartsWith("set github_client_secret "))
            {
                string val = query.Substring("set github_client_secret ".Length).Trim();
                suggestions.Add(new CommandResult
                {
                    TITLE = $"💾 Set GitHub OAuth2 Client Secret to: \"{val}\"",
                    DESCRIPTION = "Saves custom GitHub Client Secret",
                    SIMILARITY = (SearchUtil.BestSimilarity(query, "oauth", "auth", "login", "google login", "github login", "oauth2") + 7.0 * 0.01),
                    EXECUTE = () => { SettingsManager.Current.GITHUB_OAUTH_CLIENT_SECRET = val; SettingsManager.Save(); TextOverlay.Show("✅ GitHub Client Secret saved!", 2500); }
                });
            }

            return suggestions;
        }

        public List<CommandDesc> GetCommandDescriptions()
        {
            return new List<CommandDesc>
            {
                new CommandDesc("oauth / auth / login", "Open OAuth2 Account Authentication Studio", "oauth"),
                new CommandDesc("google login", "Sign in with Google OAuth2 in default browser", "google login"),
                new CommandDesc("github login", "Sign in with GitHub OAuth2 in default browser", "github login")
            };
        }
    }
}
