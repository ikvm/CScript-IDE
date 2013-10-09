using ExtendPropertyLib;
using jinx.RoslynEditor;
using Roslyn.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jinxapp
{
    public class DocumentInfo : BusinessInfoBase<DocumentInfo> 
    {
        public static ExtendProperty DocumentIDProperty = RegisterProperty(v => v.DocumentID);
        /// <summary>
        /// 文档ID
        /// </summary>
        public DocumentId DocumentID { set { SetValue(DocumentIDProperty, value); } get { return (DocumentId )GetValue(DocumentIDProperty); } }

        public static ExtendProperty TextProperty = RegisterProperty(v => v.Text);
        /// <summary>
        /// 内容
        /// </summary>
        public string Text { set { SetValue(TextProperty, value); } get { return (string )GetValue(TextProperty); } }

        public static ExtendProperty EditorProperty = RegisterProperty(v => v.Editor);
        /// <summary>
        /// 编辑器接口
        /// </summary>
        public IEditor Editor { set { SetValue(EditorProperty, value); } get { return (IEditor )GetValue(EditorProperty); } }
        /// <summary>
        /// 文档接口
        /// </summary>
        public IDocument Document { set; get; }
    }
}
