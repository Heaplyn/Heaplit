// Developer: heaplyn
// Summary: Opens the Decompile -> C Workbench (native IDA/Ghidra/RetDec decompilation with an
//          optional AI clean-up pass). "decompile", "decompile <path>", "convert to c".

using System;
using System.Collections.Generic;
using System.IO;

namespace HeaplitLauncher
{
    public class DecompileCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            string q = query.Trim().ToLower();
            return SearchUtil.MatchesAny(q, "decompile", "decompiler", "convert to c", "reverse engineer", "to c workbench");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var list = new List<CommandResult>();
            string q = query.Trim();

            // Optional path argument after the verb.
            string arg = "";
            foreach (var p in new[] { "decompile ", "decompiler ", "convert to c ", "reverse engineer " })
                if (q.StartsWith(p, StringComparison.OrdinalIgnoreCase)) { arg = q.Substring(p.Length).Trim().Trim('"'); break; }

            string desc = File.Exists(arg)
                ? $"Open {Path.GetFileName(arg)} in the Decompile → C workbench"
                : "Turn a binary into editable C (IDA ▸ Ghidra ▸ RetDec) with optional AI clean-up";

            list.Add(new CommandResult
            {
                TITLE = "🧬 Decompile → C Workbench",
                DESCRIPTION = desc,
                EXECUTE = () => DecompiledProjectOverlay.ShowOverlay(File.Exists(arg) ? arg : null),
                SIMILARITY = SearchUtil.BestSimilarity(q.ToLower(), "decompile", "convert to c", "decompiler")
            });
            return list;
        }

        public List<CommandDesc> GetCommandDescriptions() => new List<CommandDesc>
        {
            new CommandDesc("decompile", "Decompile a binary to editable C (IDA/Ghidra/RetDec + AI)", "decompile")
        };
    }
}
