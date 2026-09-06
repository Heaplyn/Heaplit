using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public interface IAiTool
    {
        string Tag { get; }
        string RegexPattern { get; }
        Task<string> ExecuteAsync(Match match, HashSet<string> executedTags);
    }
}
