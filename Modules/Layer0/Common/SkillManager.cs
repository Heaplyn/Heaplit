// Developer: heaplyn
// Date: 2026-08-17
// Summary: Autonomous Skill Evolution System.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public class HeaplitSkill
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Triggers { get; set; } = new List<string>();
        public string ActionChain { get; set; } = string.Empty;
        public string SystemInstruction { get; set; } = string.Empty;
        public string Layer { get; set; } = "Dynamic";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public static class SkillManager
    {
        private static readonly string SkillsPath = Path.Combine(PathHandler.GetDataDirectory(), "Skills.json");
        private static List<HeaplitSkill> _skills = new List<HeaplitSkill>();
        private static readonly object _lock = new object();

        static SkillManager() { LoadSkills(); }

        public static void LoadSkills() {
            try { if (File.Exists(SkillsPath)) _skills = JsonSerializer.Deserialize<List<HeaplitSkill>>(File.ReadAllText(SkillsPath)) ?? new List<HeaplitSkill>(); } catch { }
        }

        public static async Task<string> ExecuteSkillAsync(string skillName, string? input = null) {
            var skill = _skills.FirstOrDefault(s => s.Name.Equals(skillName, StringComparison.OrdinalIgnoreCase));
            if (skill == null) return "Skill not found.";

            foreach (var step in skill.ActionChain.Split('|', StringSplitOptions.RemoveEmptyEntries)) {
                string clean = step.Trim();
                if (clean.StartsWith("@")) await AiAPI.ExecuteAgentLoopInternalAsync(clean, new HashSet<string>(), new StringBuilder(), CancellationToken.None);
                else System.Windows.Application.Current.Dispatcher.Invoke(() => CommandParser.ExecuteFirstSuggestion(clean));
                await Task.Delay(200);
            }
            return "Skill executed.";
        }

        public static List<CommandResult> GetSkillSuggestions(string query) {
            return _skills.Where(s => s.Triggers.Any(t => t.ToLower().Contains(query.ToLower())))
                .Select(s => new CommandResult { TITLE = $"✨ Skill: {s.Name}", DESCRIPTION = s.Description, SIMILARITY = 5.0, EXECUTE = () => _ = ExecuteSkillAsync(s.Name) })
                .ToList();
        }
    }
}
