// Developer: heaplyn
// Date: 2026-08-17
// Summary: Command handler for interactive Help Center, command guide, and keyboard shortcut reference.

using System;
using System.Collections.Generic;

namespace HeaplitLauncher.Modules.Layer3.Handlers
{
    public class HelpCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            return SearchUtil.MatchesAny(query, "help", "guide", "shortcuts", "docs", "commands", "manual", "help center");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var results = new List<CommandResult>();

            results.Add(new CommandResult
            {
                TITLE = "📖 Open Interactive Help & Documentation Center",
                DESCRIPTION = "Browse all commands, global hotkeys, voice shortcuts, and pipeline tips",
                SIMILARITY = (SearchUtil.BestSimilarity(query, "help", "guide", "shortcuts", "docs", "commands", "manual", "help center") + 0.6 * 0.01), // Reduced from 10.0 to prevent overriding specific commands
                EXECUTE = () => HelpCenterOverlay.ShowOverlay()
            });

            results.Add(new CommandResult
            {
                TITLE = "🛠️ Repair Heaplit Documentation",
                DESCRIPTION = "Force restore and link guide files if they are missing",
                SIMILARITY = (SearchUtil.BestSimilarity(query, "help", "guide", "shortcuts", "docs", "commands", "manual", "help center") + 5.0 * 0.01),
                EXECUTE = () => RepairDocumentation()
            });

            return results;
        }

        private void RepairDocumentation()
        {
            try
            {
                string root = PathHandler.GetProjectRoot();
                string source = System.IO.Path.Combine(root, "Data", "user_guide.md");
                string dest = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "user_guide.md");

                if (System.IO.File.Exists(source))
                {
                    System.IO.File.Copy(source, dest, true);
                    TextOverlay.Show("✅ Documentation repaired and restored.", 3000);
                }
                else
                {
                    TextOverlay.Show("❌ Source guide file missing. Please rebuild.", 3000);
                }
            }
            catch (System.Exception ex) { TextOverlay.Show($"⚠️ Repair failed: {ex.Message}", 3000); }
        }

        public List<CommandDesc> GetCommandDescriptions()
        {
            var l = new List<CommandDesc>();

            // AI & LLM (10)
            l.Add(new CommandDesc("ai <prompt>", "Ask Heaplit AI assistant questions or tasks", "ai explain recursion"));
            l.Add(new CommandDesc("llm", "Open LLM Engine Studio", "llm"));
            l.Add(new CommandDesc("llm discover", "Scan network for local AI nodes (Ollama/vLLM)", "llm discover"));
            l.Add(new CommandDesc("look deep <query>", "Activate AI Deep Reasoning mode", "look deep fix this memory leak"));
            l.Add(new CommandDesc("pull r1", "Download DeepSeek R1 model via Ollama", "pull r1"));
            l.Add(new CommandDesc("pull hermes", "Download Hermes 3 model via Ollama", "pull hermes"));
            l.Add(new CommandDesc("test keys", "Test all Gemini API keys in the pool", "test keys"));
            l.Add(new CommandDesc("hf", "Open Hugging Face model hub", "hf"));
            l.Add(new CommandDesc("ai clear", "Clear current chat history", "ai clear"));
            l.Add(new CommandDesc("ai fix", "AI-assisted code fixing in editor", "ai fix"));
            l.Add(new CommandDesc("harvest datasets", "Autonomous LLM dataset discovery & download", "harvest datasets"));

            // System & Power (10)
            l.Add(new CommandDesc("lock", "Instantly lock your workstation", "lock"));
            l.Add(new CommandDesc("restart", "Restart Windows", "restart"));
            l.Add(new CommandDesc("shutdown", "Shut down the PC", "shutdown"));
            l.Add(new CommandDesc("sleep", "Put the PC into sleep mode", "sleep"));
            l.Add(new CommandDesc("debug", "Open system debugging suite", "debug"));
            l.Add(new CommandDesc("debug console", "View AI internal tool logs", "debug console"));
            l.Add(new CommandDesc("monitor", "Open resource monitor HUD", "monitor"));
            l.Add(new CommandDesc("inspect", "Inspect all running processes", "inspect"));
            l.Add(new CommandDesc("specs", "Show detailed hardware specifications", "specs"));
            l.Add(new CommandDesc("exit", "Close Heaplit completely", "exit"));

            // Media & Audio (10)
            l.Add(new CommandDesc("volume <0-100>", "Set system master volume", "volume 50"));
            l.Add(new CommandDesc("mute", "Mute system audio", "mute"));
            l.Add(new CommandDesc("tts <text>", "Speak text using active voice", "tts Online"));
            l.Add(new CommandDesc("voices", "Open TTS Voice Library", "voices"));
            l.Add(new CommandDesc("enroll voice", "Start biometric speaker verification training", "enroll voice"));
            l.Add(new CommandDesc("ffmpeg", "Open Universal Media Converter", "ffmpeg"));
            l.Add(new CommandDesc("music", "Open glassmorphic playlist manager", "music"));
            l.Add(new CommandDesc("screenshot", "Capture primary screen", "screenshot"));
            l.Add(new CommandDesc("ocr", "Analyze and extract text from screen", "ocr"));
            l.Add(new CommandDesc("stop tts", "Cancel all current speech", "stop tts"));

            // Productivity & Tools (10)
            l.Add(new CommandDesc("todo", "Open glassmorphic tasks list", "todo"));
            l.Add(new CommandDesc("remind <time> <msg>", "Schedule a local notification", "remind in 5m check oven"));
            l.Add(new CommandDesc("timer <time>", "Set a countdown timer", "timer 10m"));
            l.Add(new CommandDesc("sticky", "Create a new floating sticky note", "sticky"));
            l.Add(new CommandDesc("calc <expr>", "Solve math and symbolic calculus", "calc diff x^2"));
            l.Add(new CommandDesc("graph <expr>", "Plot a mathematical function", "graph sin(x)"));
            l.Add(new CommandDesc("clipboard", "View clipboard history", "clipboard"));
            l.Add(new CommandDesc("cal", "Open glassmorphic calendar", "cal"));
            l.Add(new CommandDesc("adhd", "Activate ADHD Focus Suite", "adhd"));
            l.Add(new CommandDesc("ideas", "Open Idea Lab brainstorming tool", "ideas"));

            // Files & Dev (10)
            l.Add(new CommandDesc("edit <path>", "Open file/folder in AI Code Studio", "edit ."));
            l.Add(new CommandDesc("push <msg>", "AI-assisted GitHub push", "push update"));
            l.Add(new CommandDesc("git status", "Show current git status", "git status"));
            l.Add(new CommandDesc("git log", "Show recent git history", "git log"));
            l.Add(new CommandDesc("build", "Open project build orchestrator", "build"));
            l.Add(new CommandDesc("ipa", "Open IPA compiler for iOS", "ipa"));
            l.Add(new CommandDesc("ps <cmd>", "Run a PowerShell command silently", "ps Get-Process"));
            l.Add(new CommandDesc("organize", "Open Visual File Organizer", "organize"));
            l.Add(new CommandDesc("view <file>", "Quick view any file content", "view readme.md"));
            l.Add(new CommandDesc("templates", "Open code template library", "templates"));
            l.Add(new CommandDesc("suite", "Open Universal Dev & Offline Suite", "suite"));

            // Customization & Web (10)
            l.Add(new CommandDesc("theme <name>", "Switch HUD visual theme", "theme cyberpunk"));
            l.Add(new CommandDesc("opacity <0.1-1>", "Adjust HUD window transparency", "opacity 0.8"));
            l.Add(new CommandDesc("settings", "Open master settings GUI", "settings"));
            l.Add(new CommandDesc("alias <key> <cmd>", "Create a custom command shortcut", "alias gs git status"));
            l.Add(new CommandDesc("bg <mode>", "Switch background (Gradient/RGB/Media)", "bg rgb"));
            l.Add(new CommandDesc("search <query>", "Search web via HUD", "search rust docs"));
            l.Add(new CommandDesc("ingest <url>", "AI-analyze and learn documentation", "ingest https://docs.rs"));
            l.Add(new CommandDesc("scrape <url>", "Scrape webpage content", "scrape example.com"));
            l.Add(new CommandDesc("oauth", "Manage API and OAuth credentials", "oauth"));
            l.Add(new CommandDesc("help", "Open this Documentation Center", "help"));

            return l;
        }

        public void OnStart() { }
    }
}
