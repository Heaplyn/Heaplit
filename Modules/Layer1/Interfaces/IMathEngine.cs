// Developer: heaplyn
// Date: 2026-08-17
// Summary: Interface for mathematical and symbolic evaluation.

using System.Collections.Generic;

namespace HeaplitLauncher
{
    public interface IMathEngine
    {
        string Evaluate(string expression);
        IReadOnlyDictionary<string, double> GetConstants();
        IReadOnlyDictionary<string, System.Func<double, double>> GetFunctions();
    }
}
