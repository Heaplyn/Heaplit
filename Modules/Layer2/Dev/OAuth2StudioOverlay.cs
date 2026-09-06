// Developer: heaplyn
// Date: 2026-08-13
// Summary: Interactive WPF Overlay for Google Gemini AI & GitHub OAuth2 Authentication.
// Provides 1-click browser OAuth2 authorization, live account status badges, and token management.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace HeaplitLauncher
{
    public class OAuth2StudioOverlay : BaseOverlay
    {
        private static OAuth2StudioOverlay? _instance;

        private TextBlock _googleStatusText = null!;
        private TextBlock _githubStatusText = null!;
        private TextBox _googleClientIdBox = null!;
        private TextBox _googleClientSecretBox = null!;
        private TextBox _githubClientIdBox = null!;
        private TextBox _githubClientSecretBox = null!;

        public static void ShowOverlay()
        {
            if (_instance == null || !_instance.IsLoaded || !_instance.IsVisible)
            {
                _instance = new OAuth2StudioOverlay();
                _instance.Show();
            }
            else
            {
                _instance.Activate();
                _instance.BringToFront();
                _instance.Focus();
            }
        }

        public OAuth2StudioOverlay() : base("🔑 OAUTH2 ACCOUNT AUTHENTICATION STUDIO", 540, 580)
        {
            this.Closed += (s, e) => { _instance = null; };

            var workArea = SystemParameters.WorkArea;
            this.Left = (workArea.Width - this.Width) / 2;
            this.Top = (workArea.Height - this.Height) / 2;

            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var root = new StackPanel { Margin = new Thickness(10) };
            scroll.Content = root;

            // ── Section 1: Google / Gemini OAuth2 ─────────────────────────────────────
            root.Children.Add(CreateHeader("🔑 Google / Gemini AI OAuth2 Account Login"));

            _googleStatusText = new TextBlock
            {
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 2, 0, 8)
            };
            root.Children.Add(_googleStatusText);

            var googleBtnGrid = new UniformGrid { Columns = 2, Margin = new Thickness(0, 0, 0, 8) };
            var btnGoogleLogin = CreateButton("🔑 Sign In with Google");
            btnGoogleLogin.FontWeight = FontWeights.Bold;
            btnGoogleLogin.Click += async (s, e) =>
            {
                btnGoogleLogin.IsEnabled = false;
                SaveCredentials();
                await OAuth2Manager.LoginGoogleOAuth2Async(status => TextOverlay.Show(status, 2500));
                RefreshStatuses();
                btnGoogleLogin.IsEnabled = true;
            };
            googleBtnGrid.Children.Add(btnGoogleLogin);

            var btnGoogleSignOut = CreateButton("🚪 Sign Out Google");
            btnGoogleSignOut.Click += (s, e) =>
            {
                OAuth2Manager.SignOutGoogle();
                RefreshStatuses();
                TextOverlay.Show("🚪 Signed out from Google Account.", 2500);
            };
            googleBtnGrid.Children.Add(btnGoogleSignOut);
            root.Children.Add(googleBtnGrid);

            root.Children.Add(CreateLabel("Custom Google OAuth2 Client ID (Optional):"));
            _googleClientIdBox = new TextBox { Text = SettingsManager.Current.GOOGLE_OAUTH_CLIENT_ID, Padding = new Thickness(4), Margin = new Thickness(0, 0, 0, 4) };
            root.Children.Add(_googleClientIdBox);

            root.Children.Add(CreateLabel("Custom Google OAuth2 Client Secret (Optional):"));
            _googleClientSecretBox = new TextBox { Text = SettingsManager.Current.GOOGLE_OAUTH_CLIENT_SECRET, Padding = new Thickness(4), Margin = new Thickness(0, 0, 0, 12) };
            root.Children.Add(_googleClientSecretBox);

            // ── Section 2: GitHub OAuth2 ──────────────────────────────────────────────
            root.Children.Add(CreateHeader("🐙 GitHub OAuth2 Account Login"));

            _githubStatusText = new TextBlock
            {
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 2, 0, 8)
            };
            root.Children.Add(_githubStatusText);

            var githubBtnGrid = new UniformGrid { Columns = 2, Margin = new Thickness(0, 0, 0, 8) };
            var btnGithubLogin = CreateButton("🐙 Sign In with GitHub");
            btnGithubLogin.FontWeight = FontWeights.Bold;
            btnGithubLogin.Click += async (s, e) =>
            {
                btnGithubLogin.IsEnabled = false;
                SaveCredentials();
                await OAuth2Manager.LoginGithubOAuth2Async(status => TextOverlay.Show(status, 2500));
                RefreshStatuses();
                btnGithubLogin.IsEnabled = true;
            };
            githubBtnGrid.Children.Add(btnGithubLogin);

            var btnGithubSignOut = CreateButton("🚪 Sign Out GitHub");
            btnGithubSignOut.Click += (s, e) =>
            {
                OAuth2Manager.SignOutGithub();
                RefreshStatuses();
                TextOverlay.Show("🚪 Signed out from GitHub Account.", 2500);
            };
            githubBtnGrid.Children.Add(btnGithubSignOut);
            root.Children.Add(githubBtnGrid);

            root.Children.Add(CreateLabel("Custom GitHub OAuth2 Client ID (Optional):"));
            _githubClientIdBox = new TextBox { Text = SettingsManager.Current.GITHUB_OAUTH_CLIENT_ID, Padding = new Thickness(4), Margin = new Thickness(0, 0, 0, 4) };
            root.Children.Add(_githubClientIdBox);

            root.Children.Add(CreateLabel("Custom GitHub OAuth2 Client Secret (Optional):"));
            _githubClientSecretBox = new TextBox { Text = SettingsManager.Current.GITHUB_OAUTH_CLIENT_SECRET, Padding = new Thickness(4), Margin = new Thickness(0, 0, 0, 12) };
            root.Children.Add(_githubClientSecretBox);

            var saveBtn = CreateButton("💾 Save OAuth2 Credentials");
            saveBtn.Height = 34;
            saveBtn.FontWeight = FontWeights.Bold;
            saveBtn.Click += (s, e) =>
            {
                SaveCredentials();
                TextOverlay.Show("✅ OAuth2 Credentials Saved!", 2500);
            };
            root.Children.Add(saveBtn);

            this.UserContent = scroll;
            RefreshStatuses();
        }

        private void SaveCredentials()
        {
            SettingsManager.Current.GOOGLE_OAUTH_CLIENT_ID = _googleClientIdBox.Text.Trim();
            SettingsManager.Current.GOOGLE_OAUTH_CLIENT_SECRET = _googleClientSecretBox.Text.Trim();
            SettingsManager.Current.GITHUB_OAUTH_CLIENT_ID = _githubClientIdBox.Text.Trim();
            SettingsManager.Current.GITHUB_OAUTH_CLIENT_SECRET = _githubClientSecretBox.Text.Trim();
            SettingsManager.Save();
        }

        private void RefreshStatuses()
        {
            string googleEmail = SettingsManager.Current.GOOGLE_OAUTH_USER_EMAIL;
            bool googleAuth = !string.IsNullOrEmpty(SettingsManager.Current.GOOGLE_OAUTH_ACCESS_TOKEN);

            _googleStatusText.Text = googleAuth
                ? $"🟢 Connected: {googleEmail}"
                : "🔴 Google OAuth2: Not Authenticated";
            _googleStatusText.Foreground = googleAuth ? Brushes.LimeGreen : Brushes.OrangeRed;

            string githubUser = SettingsManager.Current.GITHUB_OAUTH_USER_LOGIN;
            bool githubAuth = !string.IsNullOrEmpty(SettingsManager.Current.GITHUB_TOKEN);

            _githubStatusText.Text = githubAuth
                ? $"🟢 Connected: @{githubUser}"
                : "🔴 GitHub OAuth2: Not Authenticated";
            _githubStatusText.Foreground = githubAuth ? Brushes.LimeGreen : Brushes.OrangeRed;
        }

        private static TextBlock CreateHeader(string title)
        {
            var header = new TextBlock
            {
                Text = title,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 6, 0, 4)
            };
            header.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
            return header;
        }

        private static TextBlock CreateLabel(string text)
        {
            var lbl = new TextBlock
            {
                Text = text,
                FontSize = 11,
                Margin = new Thickness(0, 4, 0, 2)
            };
            lbl.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondaryBrush");
            return lbl;
        }

        private static Button CreateButton(string content)
        {
            var btn = new Button
            {
                Content = content,
                Margin = new Thickness(0, 2, 4, 4),
                Padding = new Thickness(8, 5, 8, 5),
                FontSize = 11,
                Cursor = Cursors.Hand
            };
            return btn;
        }
    }
}
