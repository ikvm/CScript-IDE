using Roslyn.Compilers.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jinxapp.DomainServices
{
    /// <summary>
    /// 语法树加载完成事件参数
    /// </summary>
    public class SyntaxLoadedArgs:EventArgs
    {
        public SyntaxLoadedArgs(CommonSyntaxNode root)
        {
            SyntaxTreeRoot = root;
        }

        public CommonSyntaxNode SyntaxTreeRoot { set; get; }

    }
}
