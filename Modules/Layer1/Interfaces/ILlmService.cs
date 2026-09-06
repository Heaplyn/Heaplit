// Developer: heaplyn
// Date: 2026-08-17
// Summary: Interface for LLM dispatching and agent loop execution.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public interface ILlmService
    {
        Task<string> AskAsync(string prompt, List<ChatTurn>? history = null, CancellationToken ct = default);
        Task<string> AskOllamaStreamAsync(string prompt, List<ChatTurn>? history, Action<string> onToken, CancellationToken ct = default);
        Task<bool> IsLocalAvailableAsync();
        Task<List<string>> GetLocalModelsAsync();
        Task<string> DiscoverAiServersAsync();
    }
}
