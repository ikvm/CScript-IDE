using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtendPropertyLib;
using ExtendPropertyLib.WPF;
using System.ComponentModel.Composition;
using MaxZhang.EasyEntities.Dynamic.Aop;
using System.Windows.Input;
using Roslyn.Scripting;
using jinx;
using Microsoft.Win32;
using System.IO;
using Roslyn.Scripting.CSharp;
using Roslyn.Compilers.CSharp;
using Roslyn.Compilers;
using Roslyn.Services;
using Roslyn.Compilers.Common;
using System.Reflection;
using System.Windows;
using jinx.RoslynEditor.RoslynExtensions;
using Editor = jinx.RoslynEditor;
using System.Collections.ObjectModel;
namespace jinxapp
{
    [Export(typeof(IShell))]
    public class MainViewModel : ViewModelBase<MainViewFormModel>, IShell
    {
        private InteractiveManager InteractiveManager = new InteractiveManager();
        private Editor.ObjectFormatter Formatter;
        private IMainView mv;
        public override void OnDoCreate(ExtendPropertyLib.ExtendObject item, params object[] args)
        {
            base.OnDoCreate(item, args);
            OpenDocuments = new ObservableCollection<DocumentInfo>();
            ApplicationService.Services.Add<InteractiveManager>(InteractiveManager);
        }

        #region ExtentProperty
      
        public static ExtendProperty CurrentDocumentProperty = RegisterProperty<MainViewModel>(v => v.CurrentDocument);
        /// <summary>
        /// 当前正在编辑的文档
        /// </summary>
        public DocumentInfo CurrentDocument { set { SetValue(CurrentDocumentProperty, value); } get { return (DocumentInfo)GetValue(CurrentDocumentProperty); } }

        public static ExtendProperty OpenDocumentsProperty = RegisterProperty<MainViewModel>(v => v.OpenDocuments);
        /// <summary>
        /// 打开文档数据源
        /// </summary>
        public ObservableCollection<DocumentInfo> OpenDocuments { set { SetValue(OpenDocumentsProperty, value); } get { return (ObservableCollection<DocumentInfo>)GetValue(OpenDocumentsProperty); } }
        
        #endregion
        private Session session;


        public override string GetViewTitle()
        {
            return "RoslynEditor - C#";
        }

        public override async void OnLoad()
        {
            mv = this.View as IMainView;
            Formatter = mv.Formatter;
            ApplicationService.Services.Add<Editor.ObjectFormatter>(Formatter);

           
            CurrentDocument = this.newDocument(null);
            OpenDocuments.Add(CurrentDocument);
            mv.AddDocument(CurrentDocument.Editor);
        }

   
        void editor_EditorTextChanged(object sender, EventArgs e)
        {
            if (CurrentDocument != null)
            {
                var doc = InteractiveManager.GetDocumentByID(CurrentDocument.DocumentID);
                mv.DisplayTree((SyntaxTree)doc.GetSyntaxTree());
            }
        }

        private bool CompileCode(ref string error)
        {
            bool compileSuccess = true;
            var model = InteractiveManager.GetCurrentDocumentSymbol();
            Diagnostic[] dg = model.GetDiagnostics().ToArray();
            
            if (dg.Length > 0)
            {
                error = "The following compile error occured:\r\n";
                foreach (Diagnostic d in dg)
                    error += "Info: " + d.Info + "\n";
                compileSuccess = false;
            }

            return compileSuccess;
        }

        //编译运行
        public void MakeAndRun()
        {
            string errorMsg = string.Empty;
            if (CompileCode(ref errorMsg))
            {
                try
                {
                    var doc = InteractiveManager.GetCurrentDocument();
                    string CSharpContent = doc.GetText().ToString();
                    var scriptEngine = InteractiveManager.GetScriptEngine();
                    session = scriptEngine.CreateSession();
                    session.Execute(CSharpContent);
                    Formatter.WriteObject("Make all success." + DateTime.Now.ToString());
                }
                catch (Exception ex)
                {
                    Formatter.WriteError(ex);
                }
            }
            else
            {
                Formatter.Clear();
                Formatter.WriteObject(errorMsg);
                MessageBox.Show("Compile Failed !", "Tips", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        //构建Javascript
        public void Build()
        {
            //string js = JavaScriptCompiler.EmitJs(this.CSharpContent);
            //this.JSharpContent = js;

        }
     
        //打开文件
        public void Open()
        {
            OpenFileDialog odf = new OpenFileDialog();
            odf.Filter = "csharp files (*.cs)|*.cs|All files (*.*)|*.*";
            if (odf.ShowDialog().Value)
            {
                string fileName = odf.FileName;
                string content = File.ReadAllText(fileName, Encoding.UTF8);
               
            }
        }

        //新建文件
        public void New()
        {
            if (MessageBox.Show("新建文件，要继续操作吗？", "提示"
                                    , MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                string content = File.ReadAllText("NewFile.cs", Encoding.UTF8);
                var doc = this.newDocument(content);
                OpenDocuments.Add(doc);
                mv.AddDocument(doc.Editor);
            }
        }

        private DocumentInfo newDocument(string text)
        {
            mv = this.View as IMainView;
            var editor = mv.CreateEditor(text);
            var document = new DocumentInfo();
            var id = document.DocumentID = InteractiveManager.CreateAndOpenDocument(editor.TextContainer);
            editor.DocumentID = id;
            editor.EditorTextChanged += editor_EditorTextChanged;
            document.Editor = editor;
            return document;
        }

        public bool CloseDocument(DocumentId id)
        {
            if (OpenDocuments.Count > 1)
            {
                var docInfo = OpenDocuments.First(d => d.DocumentID == id);
                docInfo.Editor.EditorTextChanged -= editor_EditorTextChanged;
                OpenDocuments.Remove(docInfo);
                CurrentDocument = OpenDocuments[0];
                return true;
            }
            return false;
        }

        public void SetCurrentDocument(DocumentId id)
        {
            CurrentDocument = OpenDocuments.First(d => d.DocumentID == id);
            InteractiveManager.SetCurrentDocumentByID(id);
        }

    }
}
