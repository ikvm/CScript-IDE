using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jinx.RoslynEditor.RoslynExtensions
{
    /// <summary>
    /// An item in the insight window.
    /// </summary>
    public interface IInsightItem
    {
        object Header { get; }
        object Content { get; }
    }
}
