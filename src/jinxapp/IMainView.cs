using jinx.RoslynEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace jinxapp
{
    interface IMainView
    {
        ObjectFormatter Formatter { get; }
    }
}
