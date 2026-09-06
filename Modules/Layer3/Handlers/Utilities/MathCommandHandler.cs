// Developer: heaplyn
// Date: 2026-08-08
// Summary: Parses and calculates mathematical string queries using the modular MathEngine.

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;

namespace HeaplitLauncher
{
    public class MathCommandHandler : ICommandHandler
    {
        private static readonly string[] MathFuncs =
            { "sin", "cos", "tan", "asin", "acos", "atan", "sqrt", "abs", "ln", "log", "exp", "floor", "ceil" };

        /// <summary>An expression "looks like math" if it calls a known function, or has a digit plus
        /// an arithmetic operator / parentheses. Bare numbers and prose are ignored so we don't spam
        /// the results list.</summary>
        internal static bool LooksLikeMath(string q)
        {
            if (string.IsNullOrWhiteSpace(q)) return false;
            q = q.ToLower();
            bool hasFunc  = MathFuncs.Any(f => q.Contains(f + "("));
            bool hasDigit = q.Any(char.IsDigit);
            bool hasOp    = q.IndexOfAny(new[] { '+', '*', '/', '^' }) >= 0
                            || Regex.IsMatch(q, @"\d\s*-\s*[\d\.\(]");   // subtraction between numbers (not a stray dash)
            bool hasParen = q.Contains('(') && q.Contains(')');
            return hasFunc || (hasDigit && (hasOp || hasParen));
        }

        public bool CanHandle(string query)
        {
            string q = query.Trim().ToLower();
            if (q == "calc" || q == "calculus" || q.StartsWith("calc ")
                || q.Contains("integrate") || q.Contains("derivative") || q.StartsWith("diff ")) return true;
            return LooksLikeMath(StripPrefix(q));
        }

        // Allow natural lead-ins: "calc 2+2", "= 2+2", "solve 2+2", "what is 2+2".
        private static string StripPrefix(string q)
        {
            q = q.Trim();
            foreach (var p in new[] { "calc ", "calculate ", "= ", "solve ", "what is ", "whats ", "eval " })
                if (q.StartsWith(p, StringComparison.OrdinalIgnoreCase)) { q = q.Substring(p.Length).Trim(); break; }
            return q.TrimEnd('=', '?').Trim();
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            string clean = query.Trim().ToLower();

            // 1. Explicit Calculus Studio launcher
            if (clean == "calc" || clean == "calculus")
            {
                suggestions.Add(new CommandResult {
                    TITLE = "📐 Open Calculus Studio",
                    DESCRIPTION = "Launch the advanced symbolic math and calculus solver",
                    EXECUTE = () => CalculusStudioOverlay.ShowStudio(),
                    SIMILARITY = 10.0
                });
                return suggestions;
            }

            // 2. Calculus / symbolic queries -> Studio
            if (clean.Contains("integrate") || clean.Contains("derivative") || clean.Contains("limit of"))
            {
                suggestions.Add(new CommandResult {
                    TITLE = "🧠 Solve in Calculus Studio",
                    DESCRIPTION = $"Solve '{query}' with the symbolic engine",
                    EXECUTE = () => CalculusStudioOverlay.ShowStudio(),
                    SIMILARITY = 9.5
                });
            }

            // 3. Arithmetic / functions / constants -> inline answer
            try
            {
                string expr = StripPrefix(clean);
                string result = CoreRegistry.Intelligence.Math.Evaluate(expr);

                // Only surface a genuine numeric answer; engine "error"/symbolic strings are skipped.
                if (IsNumericResult(result))
                {
                    suggestions.Add(new CommandResult
                    {
                        TITLE = $"🟰 {expr} = {result}",
                        DESCRIPTION = "Click to copy the result",
                        EXECUTE = () => { try { System.Windows.Clipboard.SetText(result); TextOverlay.Show($"📋 Copied {result}", 1500); } catch { } },
                        SIMILARITY = 9.0
                    });
                }
                else if (clean.StartsWith("diff "))
                {
                    // Symbolic derivative result (e.g. "diff 3x^2" -> "6x")
                    suggestions.Add(new CommandResult
                    {
                        TITLE = $"d/dx = {result}",
                        DESCRIPTION = "Offline power-rule derivative (click to copy)",
                        EXECUTE = () => { try { System.Windows.Clipboard.SetText(result); } catch { } },
                        SIMILARITY = 8.5
                    });
                }
            }
            catch { }

            return suggestions;
        }

        private static bool IsNumericResult(string s)
            => !string.IsNullOrWhiteSpace(s)
               && double.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out _);

        public List<CommandDesc> GetCommandDescriptions()
        {
            return new List<CommandDesc>
            {
                new CommandDesc("calc", "Launch Calculus Studio", "calc"),
                new CommandDesc("5 + 5 * 2", "Quick math result", "10 + 10"),
                new CommandDesc("diff 3x^2", "Offline derivative", "diff x^3")
            };
        }

        public void OnStart() { }
    }
}
