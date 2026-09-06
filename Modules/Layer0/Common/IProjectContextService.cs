// Developer: heaplyn
// Date: 2026-08-17
// Summary: Interface for deep project context gathering and analysis.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public class FileSummary
    {
        public string FilePath { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public long Size { get; set; }
    }

    public interface IProjectContextService
    {
        Task RefreshIndexAsync(string rootPath);
        Task<string> GetProjectSummaryAsync();
        Task RunDeepAnalysisAsync(System.Action<string, double> progressCallback);
        List<FileSummary> GetFileSummaries();
    }
}
