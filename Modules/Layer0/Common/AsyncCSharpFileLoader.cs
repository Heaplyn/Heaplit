// Developer: heaplyn
// Date: 2026-08-15
// Summary: Lightweight C# file structure parser using Regex to avoid heavy Roslyn dependencies.
//          Provides a basic method and type outline for the built-in Text Editor.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public sealed class AsyncCSharpFileLoader
    {
        public async Task<FileOutline> LoadFileOutlineAsync(string FilePath, CancellationToken CancellationToken = default)
        {
            if (!File.Exists(FilePath)) return new FileOutline(FilePath, new List<TypeOutline>());

            string text = await File.ReadAllTextAsync(FilePath, CancellationToken).ConfigureAwait(false);

            var types = new List<TypeOutline>();
            var lines = text.Split('\n');

            // Simple Regex patterns for classes and methods
            var classRegex = new Regex(@"\b(?:public|private|internal|protected)?\s+(?:static|partial)?\s*(?:class|struct|interface|enum)\s+([a-zA-Z0-9_]+)", RegexOptions.Compiled);
            var methodRegex = new Regex(@"\b(?:public|private|internal|protected)?\s+(?:static|async|virtual|override|abstract)?\s*([a-zA-Z0-9_<>]+)\s+([a-zA-Z0-9_]+)\s*\((.*?)\)", RegexOptions.Compiled);

            TypeOutline? currentType = null;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//") || line.StartsWith("/*")) continue;

                var classMatch = classRegex.Match(line);
                if (classMatch.Success)
                {
                    string className = classMatch.Groups[1].Value;
                    string kind = line.Contains("class") ? "class" : line.Contains("struct") ? "struct" : "interface";
                    currentType = new TypeOutline(className, kind, new List<MethodOutline>());
                    types.Add(currentType);
                    continue;
                }

                var methodMatch = methodRegex.Match(line);
                if (methodMatch.Success && currentType != null)
                {
                    string returnType = methodMatch.Groups[1].Value;
                    string methodName = methodMatch.Groups[2].Value;
                    string paramStr = methodMatch.Groups[3].Value;

                    // Filter out common false positives like 'if', 'while', 'using'
                    if (new[] { "if", "while", "for", "foreach", "using", "lock", "switch", "catch" }.Contains(methodName)) continue;

                    var parameters = new List<ParameterOutline>();
                    if (!string.IsNullOrWhiteSpace(paramStr))
                    {
                        var parts = paramStr.Split(',');
                        foreach (var p in parts)
                        {
                            var pParts = p.Trim().Split(' ');
                            if (pParts.Length >= 2)
                                parameters.Add(new ParameterOutline(pParts.Last(), pParts[0]));
                        }
                    }

                    currentType.Methods.Add(new MethodOutline(methodName, returnType, parameters, i + 1));
                }
            }

            return new FileOutline(Path.GetFullPath(FilePath), types);
        }

        // Removed heavy Roslyn compilation and invocation logic to keep EXE size small.
        public Task<object?> InvokeMethodAsync(string p1, string p2, string p3, object?[]? p4, CancellationToken ct)
            => Task.FromResult<object?>(null);
    }

    public sealed record FileOutline(string FILE_PATH, List<TypeOutline> TYPES)
    {
        public string FilePath => FILE_PATH;
        public List<TypeOutline> Types => TYPES;
    }

    public sealed record TypeOutline(string NAME, string KIND, List<MethodOutline> METHODS)
    {
        public string Name => NAME;
        public string Kind => KIND;
        public List<MethodOutline> Methods => METHODS;
    }

    public sealed record MethodOutline(string NAME, string RETURN_TYPE, List<ParameterOutline> PARAMETERS, int LINE_NUMBER)
    {
        public string Name => NAME;
        public string ReturnType => RETURN_TYPE;
        public List<ParameterOutline> Parameters => PARAMETERS;
        public int LineNumber => LINE_NUMBER;
    }

    public sealed record ParameterOutline(string NAME, string TYPE)
    {
        public string Name => NAME;
        public string Type => TYPE;
    }
}
