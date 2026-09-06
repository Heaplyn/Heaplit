// Developer: heaplyn
// Date: 2026-08-16
// Summary: Offline Code Analysis Manager.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace HeaplitLauncher
{
    public class CodeError
    {
        public int Line { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Severity { get; set; } = "Error";
        public override string ToString() => $"[{Severity}] Line {Line}: {Message}";
    }

    public static class EditorAnalysisManager
    {
        public static List<CodeError> Analyze(string text, string extension)
        {
            var errors = new List<CodeError>();
            string[] lines = text.Split('\n');
            if (extension.Equals(".cs", StringComparison.OrdinalIgnoreCase))
            {
                int open = Regex.Matches(text, "{").Count;
                int close = Regex.Matches(text, "}").Count;
                if (open != close) errors.Add(new CodeError { Line = lines.Length, Message = $"Unbalanced braces: {open} open vs {close} close.", Severity = "Error" });
                for (int i = 0; i < lines.Length; i++) {
                    string l = lines[i].Trim();
                    if (string.IsNullOrWhiteSpace(l) || l.StartsWith("//") || l.StartsWith("using")) continue;
                    if (!l.EndsWith(";") && !l.EndsWith("{") && !l.EndsWith("}") && !Regex.IsMatch(l, @"\b(class|namespace|if|else|foreach|while|for|using)\b"))
                        errors.Add(new CodeError { Line = i + 1, Message = "Potential missing semicolon.", Severity = "Warning" });
                }
            }
            return errors;
        }

        public static string GetAiBoostPrompt(string text, List<CodeError> errors)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Detected errors:");
            foreach (var err in errors) sb.AppendLine($"- Line {err.Line}: {err.Message}");
            sb.AppendLine("\nCODE:\n" + text);
            return sb.ToString();
        }
    }
}
