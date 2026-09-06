// Developer: heaplyn
// Date: 2026-08-17
// Summary: Interface for system memory and context management.

namespace HeaplitLauncher
{
    public interface IMemoryService
    {
        void Start();
        void Stop();
        string GetCurrentWindowTitle();
    }
}
