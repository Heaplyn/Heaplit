// Developer: heaplyn
// Date: 2026-08-17
// Summary: Advanced Editor Intelligence Manager.
//          Enhanced Assembly (NASM) support with struct/directive highlighting.
//          Added support for C++, SQL, Lua, and more.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public class AutocompleteSuggestion
    {
        public string Text { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = "📄";
        public double Score { get; set; } = 0;
    }

    public class SyntaxRule
    {
        public string Pattern { get; set; } = string.Empty;
        public string ColorHex { get; set; } = "#FFFFFF";
        public bool IsBold { get; set; } = false;
        public string Category { get; set; } = "General";
    }

    public static class EditorIntelligenceManager
    {
        public static Dictionary<string, List<SyntaxRule>> SyntaxHighlightingRules = new Dictionary<string, List<SyntaxRule>>(StringComparer.OrdinalIgnoreCase)
        {
            { ".cs", new List<SyntaxRule> {
                new SyntaxRule { Pattern = @"\b(public|private|protected|internal|static|void|string|int|bool|var|if|else|foreach|while|return|class|namespace|using|async|await|task|override|virtual|new|get|set|value|delegate|event)\b", ColorHex = "#569CD6", IsBold = true },
                new SyntaxRule { Pattern = @"\b(Console|Task|List|Dictionary|Enumerable|DateTime|Guid|Thread|Regex|HttpClient|JsonSerializer|File|Directory|Path|Math|Exception)\b", ColorHex = "#4EC9B0" },
                new SyntaxRule { Pattern = @"//.*", ColorHex = "#6A9955" },
                new SyntaxRule { Pattern = @"""[^""\\]*(?:\\.[^""\\]*)*""", ColorHex = "#D69D85" },
                new SyntaxRule { Pattern = @"\b\d+\b", ColorHex = "#B5CEA8" }
            }},
            { ".cpp", new List<SyntaxRule> {
                new SyntaxRule { Pattern = @"\b(int|double|float|char|bool|void|class|struct|union|enum|public|private|protected|static|virtual|override|final|inline|constexpr|namespace|using|template|auto|new|delete|try|catch|throw|if|else|for|while|do|switch|case|default|break|continue|return|this|nullptr|true|false)\b", ColorHex = "#569CD6", IsBold = true },
                new SyntaxRule { Pattern = @"\b(std|vector|string|map|set|list|iostream|fstream|printf|scanf|cout|cin|endl)\b", ColorHex = "#4EC9B0" },
                new SyntaxRule { Pattern = @"//.*|/\*[\s\S]*?\*/", ColorHex = "#6A9955" },
                new SyntaxRule { Pattern = @"#\s*(include|define|if|ifdef|ifndef|else|endif|pragma)", ColorHex = "#9B9B9B" },
                new SyntaxRule { Pattern = @"""[^""\\]*(?:\\.[^""\\]*)*""|'[^'\\ ]*(?:\\.[^'\\ ]*)*'", ColorHex = "#D69D85" },
                new SyntaxRule { Pattern = @"\b\d+(\.\d+)?f?\b", ColorHex = "#B5CEA8" }
            }},
            { ".h", new List<SyntaxRule> {
                new SyntaxRule { Pattern = @"\b(class|struct|public|private|protected|static|virtual|void|int|float|double|char|bool|namespace)\b", ColorHex = "#569CD6", IsBold = true },
                new SyntaxRule { Pattern = @"//.*|/\*[\s\S]*?\*/", ColorHex = "#6A9955" },
                new SyntaxRule { Pattern = @"#\s*(include|define|ifndef|endif|pragma)", ColorHex = "#9B9B9B" }
            }},
            { ".asm", new List<SyntaxRule> {
                // Instructions
                new SyntaxRule { Pattern = @"\b(mov|add|sub|inc|dec|mul|div|jmp|je|jne|jg|jl|jge|jle|cmp|push|pop|call|ret|int|syscall|nop|lea|xor|and|or|not|shl|shr)\b", ColorHex = "#569CD6", IsBold = true },
                // Directives
                new SyntaxRule { Pattern = @"\b(equ|resb|resw|resd|resq|db|dw|dd|dq|bits|section|global|extern|align|times|org|struc|endstruc|struct)\b", ColorHex = "#D8A0DF" },
                // Registers
                new SyntaxRule { Pattern = @"\b(eax|ebx|ecx|edx|esi|edi|esp|ebp|rax|rbx|rcx|rdx|rsi|rdi|rsp|rbp|ax|bx|cx|dx|si|di|sp|bp|al|ah|bl|bh|cl|ch|dl|dh|r\d+[dbw]?|xmm\d+|ymm\d+|zmm\d+|cs|ds|es|fs|gs|ss)\b", ColorHex = "#9CDCFE" },
                // Comments
                new SyntaxRule { Pattern = @";.*", ColorHex = "#6A9955" },
                // Struct members / labels starting with dot (e.g. .Type)
                new SyntaxRule { Pattern = @"(?<=\s|^)\.\w+", ColorHex = "#4EC9B0" },
                // Strings
                new SyntaxRule { Pattern = @"'[^']*'|""[^""]*""", ColorHex = "#D69D85" },
                // Numbers
                new SyntaxRule { Pattern = @"\b(0x[0-9a-fA-F]+|[0-9]+h?)\b", ColorHex = "#B5CEA8" }
            }},
            { ".lua", new List<SyntaxRule> {
                new SyntaxRule { Pattern = @"\b(and|break|do|else|elseif|end|false|for|function|if|in|local|nil|not|or|repeat|return|then|true|until|while)\b", ColorHex = "#569CD6", IsBold = true },
                new SyntaxRule { Pattern = @"\b(print|math|string|table|require)\b", ColorHex = "#4EC9B0" },
                new SyntaxRule { Pattern = @"--.*|--\[\[[\s\S]*?\]\]", ColorHex = "#6A9955" },
                new SyntaxRule { Pattern = @"""[^""]*""|'[^']*'|\[\[[\s\S]*?\]\]", ColorHex = "#D69D85" }
            }},
            { ".sql", new List<SyntaxRule> {
                new SyntaxRule { Pattern = @"\b(SELECT|FROM|WHERE|INSERT|INTO|UPDATE|DELETE|CREATE|TABLE|DROP|ALTER|JOIN|ON|GROUP|BY|ORDER|VALUES|AND|OR|NOT|AS|PRIMARY|KEY)\b", ColorHex = "#569CD6", IsBold = true },
                new SyntaxRule { Pattern = @"\b(int|varchar|nvarchar|text|date|datetime|bit|decimal)\b", ColorHex = "#4EC9B0" },
                new SyntaxRule { Pattern = @"--.*|/\*[\s\S]*?\*/", ColorHex = "#6A9955" },
                new SyntaxRule { Pattern = @"'[^']*'", ColorHex = "#D69D85" }
            }}
        };

        private static readonly Dictionary<string, string[]> LanguageKeywords = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            { ".cs", new[] { "public", "private", "protected", "internal", "static", "void", "string", "int", "bool", "var", "if", "else", "foreach", "while", "return", "class", "namespace", "using", "async", "await", "task" } },
            { ".cpp", new[] { "int", "double", "float", "char", "bool", "void", "class", "struct", "public", "private", "protected", "static", "virtual", "return", "if", "else", "for", "while" } },
            { ".lua", new[] { "and", "break", "do", "else", "elseif", "end", "false", "for", "function", "if", "in", "local", "nil", "not", "or", "repeat", "return", "then", "true", "until", "while" } },
            { ".sql", new[] { "SELECT", "FROM", "WHERE", "INSERT", "UPDATE", "DELETE", "CREATE", "DROP", "TABLE", "JOIN", "VALUES" } },
            { ".asm", new[] { "mov", "add", "sub", "inc", "dec", "jmp", "je", "jne", "cmp", "push", "pop", "call", "ret", "equ", "resb", "resw", "resd", "resq", "bits", "section", "struc", "endstruc" } }
        };

        public static List<AutocompleteSuggestion> GetSuggestions(string currentLinePrefix, string extension, string fullText)
        {
            var results = new List<AutocompleteSuggestion>();
            string lastWord = Regex.Match(currentLinePrefix, @"\b\w*$").Value;
            if (string.IsNullOrEmpty(lastWord)) return results;

            if (LanguageKeywords.TryGetValue(extension, out var keywords))
            {
                foreach (var kw in keywords.Where(k => k.StartsWith(lastWord, StringComparison.OrdinalIgnoreCase)))
                {
                    results.Add(new AutocompleteSuggestion { Text = kw, Description = "Keyword", Icon = "🔑", Score = 1.0 });
                }
            }

            var localSymbols = ExtractLocalSymbols(fullText, extension);
            foreach (var s in localSymbols.Where(s => s.Text.StartsWith(lastWord, StringComparison.OrdinalIgnoreCase)))
            {
                if (results.Any(r => r.Text == s.Text)) continue;
                s.Score = 0.8;
                results.Add(s);
            }

            return results.OrderByDescending(r => r.Score).Take(15).ToList();
        }

        public static async Task<string> GetAiExplanationAsync(string symbol, string codeContext, string extension)
        {
            try
            {
                string prompt = $"### TASK\nBriefly explain what the symbol '{symbol}' does in the context of this {extension} code. " +
                               "If it looks like a variable, describe its likely purpose. If it's a keyword, explain its function.\n\n" +
                               "### CONTEXT\n" + codeContext.TakeLast(1000) + "\n\n" +
                               "### RULES\n1. Be extremely concise (10 words max).\n2. No preamble.";

                return await LlmRouter.AskAsync(prompt);
            }
            catch { return "No explanation available."; }
        }

        public static List<AutocompleteSuggestion> ExtractLocalSymbols(string text, string extension)
        {
            var symbols = new HashSet<string>();
            var matches = Regex.Matches(text, @"\b[a-zA-Z_][a-zA-Z0-9_]{3,}\b");
            foreach (Match m in matches) symbols.Add(m.Value);
            return symbols.Select(s => new AutocompleteSuggestion { Text = s, Description = "Local Symbol", Icon = "💎" }).ToList();
        }
    }
}
