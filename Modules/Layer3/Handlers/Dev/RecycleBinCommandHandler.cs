// Developer: heaplyn
// Date: 2026-08-09
// Summary: Handles system cleaning commands to empty the Windows Recycle Bin using Shell32.

using System;
using System.Collections.Generic;

namespace HeaplitLauncher
{
    public class RecycleBinCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            query = query.Trim().ToLower();
            return SearchUtil.IsClose(query, "empty") || 
                   SearchUtil.IsClose(query, "emptyrecycle") || 
                   SearchUtil.IsClose(query, "trash") ||
                   SearchUtil.IsClose(query, "emptybin");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            query = query.Trim().ToLower();

            double similarity = Math.Max(
                SearchUtil.GetSimilarity(query, "empty"),
                Math.Max(SearchUtil.GetSimilarity(query, "trash"), SearchUtil.GetSimilarity(query, "emptybin"))
            );

            suggestions.Add(new CommandResult
            {
                TITLE = "Empty Recycle Bin",
                DESCRIPTION = "Permanently delete all items in the Recycle Bin",
                EXECUTE = () => EmptyBin(),
                SIMILARITY = similarity
            });

            return suggestions;
        }

        private static void EmptyBin()
        {
            try
            {
                uint flags = NativeMethods.SHERB_NOCONFIRMATION | NativeMethods.SHERB_NOPROGRESSUI | NativeMethods.SHERB_NOSOUND;
                int result = NativeMethods.SHEmptyRecycleBin(IntPtr.Zero, null, flags);
                if (result == 0)
                {
                    TextOverlay.Show("🗑️ Recycle Bin emptied successfully!", 2500);
                }
                else
                {
                    TextOverlay.Show("🗑️ Recycle Bin is already empty!", 2500);
                }
            }
            catch (Exception ex)
            {
                TextOverlay.Show($"⚠️ Failed to empty Recycle Bin: {ex.Message}", 3000);
            }
        }
    }
}
