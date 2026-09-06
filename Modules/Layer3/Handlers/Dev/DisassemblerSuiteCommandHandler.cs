// Developer: heaplyn
// Date: 2026-08-20
// Summary: Command handler that opens the Disassembler Suite overlay for binary analysis, PE parsing, and MSIL decompilation.

using System;
using System.Collections.Generic;

namespace HeaplitLauncher.Modules.Layer3.Handlers
{
    public class DisassemblerSuiteCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            query = query.Trim().ToLower();
            return SearchUtil.IsClose(query, "disasm") ||
                   SearchUtil.IsClose(query, "disassemble") ||
                   SearchUtil.IsClose(query, "disassembler") ||
                   SearchUtil.IsClose(query, "decompiler") ||
                   SearchUtil.IsClose(query, "pe info") ||
                   SearchUtil.IsClose(query, "hex view") ||
                   SearchUtil.IsClose(query, "peinfo") ||
                   SearchUtil.IsClose(query, "hexview");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            query = query.Trim().ToLower();

            double similarity = 0;
            if (SearchUtil.IsClose(query, "disasm") ||
                SearchUtil.IsClose(query, "disassemble") ||
                SearchUtil.IsClose(query, "disassembler") ||
                SearchUtil.IsClose(query, "decompiler"))
            {
                similarity = Math.Max(similarity, SearchUtil.GetSimilarity(query, "disasm"));
                similarity = Math.Max(similarity, SearchUtil.GetSimilarity(query, "disassemble"));
                similarity = Math.Max(similarity, SearchUtil.GetSimilarity(query, "disassembler"));
                similarity = Math.Max(similarity, SearchUtil.GetSimilarity(query, "decompiler"));
                if (similarity < 5.0) similarity = 5.0;
            }
            else if (SearchUtil.IsClose(query, "pe info") ||
                     SearchUtil.IsClose(query, "hex view") ||
                     SearchUtil.IsClose(query, "peinfo") ||
                     SearchUtil.IsClose(query, "hexview"))
            {
                similarity = Math.Max(similarity, SearchUtil.GetSimilarity(query, "pe info"));
                similarity = Math.Max(similarity, SearchUtil.GetSimilarity(query, "hex view"));
                similarity = Math.Max(similarity, SearchUtil.GetSimilarity(query, "peinfo"));
                similarity = Math.Max(similarity, SearchUtil.GetSimilarity(query, "hexview"));
                if (similarity < 4.0) similarity = 4.0;
            }

            suggestions.Add(new CommandResult
            {
                TITLE = "🛠️ Open Disassembler Suite",
                DESCRIPTION = "Analyze PE headers, disassemble .NET IL, view raw file hex dumps, and run native assembly checks",
                SIMILARITY = similarity + 1.0,
                EXECUTE = () => DisassemblerSuiteOverlay.ShowOverlay()
            });

            return suggestions;
        }

        public List<CommandDesc> GetCommandDescriptions()
        {
            return new List<CommandDesc>
            {
                new CommandDesc("disasm", "Open the visual Disassembler Suite", "disasm"),
                new CommandDesc("decompiler", "Open .NET decompiler and assembly viewer", "decompiler"),
                new CommandDesc("peinfo", "Analyze PE/ELF executable headers and imports", "peinfo"),
                new CommandDesc("hexview", "Open visual file hex viewer", "hexview")
            };
        }
    }
}
