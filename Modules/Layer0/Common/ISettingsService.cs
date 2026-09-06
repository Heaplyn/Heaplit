// Developer: heaplyn
// Date: 2026-08-17
// Summary: Interface for global system settings management.

namespace HeaplitLauncher
{
    public interface ISettingsService
    {
        SystemSettings Current { get; }
        void Load();
        void Save();
    }
}
