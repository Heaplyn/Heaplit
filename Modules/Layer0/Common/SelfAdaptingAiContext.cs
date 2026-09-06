// Developer: heaplyn
// Date: 2026-08-14
// Summary: Gathers and builds dynamic contextual telemetry representing the user's current environment.
// Automatically adapts AI prompts based on active files, running processes, screen clutter, and mobile pairing states.

using System;
using System.IO;
using System.Linq;
using System.Text;

namespace HeaplitLauncher
{
    public static class SelfAdaptingAiContext
    {
        public static string BuildDynamicAdaptiveContext()
        {
            var sb = new StringBuilder();
            sb.AppendLine("## DYNAMIC HUD TELEMETRY & USER ENVIRONMENT");

            // 0. SELF-REFERENTIAL UNDERSTANDING
            sb.AppendLine("- My Identity: I am the Heaplit HUD Assistant, a custom-built C# .NET desktop overlay.");
            sb.AppendLine($"- Local Time: {DateTime.Now:F}");
            sb.AppendLine("- Active Capabilities: Real-time screen analysis, file manipulation, script execution, mobile pairing, and system control.");
            sb.AppendLine("- System State: Fully integrated with Windows shell and specialized developer tools.");

            // 1. Detect active coding ecosystem
            string activeWorkspace = CodeAssistManager.ActiveCodebasePath;
            sb.AppendLine($"- Active Codebase Workspace: '{activeWorkspace}'");

            bool hasRoblox = Directory.Exists(Path.Combine(activeWorkspace, "Rings")) || 
                             Directory.GetFiles(activeWorkspace, "*.lua", SearchOption.AllDirectories).Any() ||
                             Directory.GetFiles(activeWorkspace, "*.luau", SearchOption.AllDirectories).Any();
                             
            bool hasMaui = Directory.GetFiles(activeWorkspace, "*.csproj", SearchOption.AllDirectories)
                                    .Any(f => File.ReadAllText(f).Contains("net8.0-ios") || File.ReadAllText(f).Contains("UseMaui"));

            if (hasRoblox)
            {
                sb.AppendLine("- User Task Profile: 🎮 ROBLOX GAME DEVELOPER (Luau, Rojo, Rings architecture). Maintain Roblox Ring rules.");
            }
            else if (hasMaui)
            {
                sb.AppendLine("- User Task Profile: 📱 MOBILE APP DEVELOPER (.NET MAUI / iOS, IPA building, Sideloadly deploying). Focus on Xamarin/MAUI advice.");
            }
            else
            {
                sb.AppendLine("- User Task Profile: 💻 GENERAL WINDOWS POWER USER / SOFTWARE ENGINEER.");
            }

            // 2. Add Screen Monitor context
            ScreenMonitorEngine.UpdateActiveWindowInfo();
            string activeWindow = ScreenMonitorEngine.ActiveWindowTitle;
            sb.AppendLine($"- Foreground Active Window: '{activeWindow}'");

            // 3. Add Mobile Pairing stats
            bool mobileActive = MobileBridgeServer.IsActive;
            sb.AppendLine($"- Mobile Companion Hub Status: {(mobileActive ? "🟢 Connected" : "🔴 Disconnected")}");
            if (mobileActive)
            {
                sb.AppendLine($"- Mobile Server API Link: {MobileBridgeServer.ServerUrl}");
            }

            // 4. Add Sideloadly status
            sb.AppendLine($"- iOS Sideloader (Sideloadly): {(SideloadlyIntegrator.IsInstalled ? "🟢 Installed" : "🔴 Not Installed (needs sideloadly.exe)")}");

            // 5. Add Self-Taught System Knowledge
            string knowledge = SystemKnowledgeManager.GetSystemKnowledge();
            if (!string.IsNullOrEmpty(knowledge))
            {
                sb.AppendLine("\n" + knowledge);
            }

            // 6. Add Clipboard peek to predict active intent
            try
            {
                string clip = System.Windows.Clipboard.GetText().Trim();
                if (!string.IsNullOrEmpty(clip))
                {
                    string preview = clip.Length > 160 ? clip.Substring(0, 160) + "..." : clip;
                    sb.AppendLine($"- Recent User Clipboard Text: \"{preview.Replace("\r", " ").Replace("\n", " ")}\"");
                }
            }
            catch { }

            sb.AppendLine("Use these details to tailor your response. Be direct and adapt code suggestions to these languages and paths without prompting.");
            return sb.ToString();
        }
    }
}
