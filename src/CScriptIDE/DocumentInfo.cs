using ExtendPropertyLib;
using CScriptIDE.RoslynEditor;
using CScriptIDE.RoslynEditor.RoslynExtensions;
using Roslyn.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CScriptIDE
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
        public IDocument Document { 
            get
            {
                var im = ApplicationService.Services.Take<InteractiveManager>();
                return im.GetDocumentByID(this.DocumentID);
            }
        
        }
        /// <summary>
        /// 标题
        /// </summary>
        public string Title { set; get; }

        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { set; get; }

        /// <summary>
        /// 是否在文件系统中保存过
        /// </summary>
        [DefaultValue(false)]
        public bool IsFileSystemSaved { set; get; }
        /// <summary>
        /// 目前文档的保存状态，指是否再次编辑过，当前的编辑状态。
        /// </summary>
         [DefaultValue(false)]
        public bool IsSaving { set; get; }

    }
}
