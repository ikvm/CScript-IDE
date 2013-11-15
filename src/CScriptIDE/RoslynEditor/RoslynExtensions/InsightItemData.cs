using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CScriptIDE.RoslynEditor.RoslynExtensions
{
    public class InsightItemData:IInsightItem
    {
        public InsightItemData(string header, string content)
        {
            this.Header = header;
            this.Content = content;
        }

        public object Header
        {
            private set;get;
        }

        public object Content
        {
            private set;get;
        }
    }
}
