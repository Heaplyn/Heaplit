// Developer: heaplyn
// Date: 2026-08-17
// Summary: Interface for web scraping, downloads, and search.

using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public interface IWebOperationService
    {
        Task<string> SearchWebAsync(string query);
        Task<string> ScrapeWebpageAsync(string url);
        Task<string> DownloadFileAsync(string url, string? destPath = null);
        Task<string> IngestDocumentationAsync(string url);
    }
}
