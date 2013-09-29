using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtendPropertyLib;
using ExtendPropertyLib.WPF;
using System.ComponentModel.Composition;
using MaxZhang.EasyEntities.Dynamic.Aop;
using System.Windows.Input;
using RoslynPad.RoslynExtensions;
using Roslyn.Scripting;
using jinx;
using Microsoft.Win32;
using System.IO;
using Roslyn.Scripting.CSharp;
using Roslyn.Compilers.CSharp;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using System.Reflection;
using System.Windows;

namespace jinxapp
{
    [Export(typeof(IShell))]
    public class MainViewModel : ViewModelBase<MainViewFormModel>, IShell
    {

        public override void OnDoCreate(ExtendPropertyLib.ExtendObject item, params object[] args)
        {
            base.OnDoCreate(item, args);

        }

        //JS脚本内容
        public static ExtendProperty JSharpContentProperty = RegisterProperty<MainViewModel>(v => v.JSharpContent);
        public string JSharpContent { set { SetValue(JSharpContentProperty, value); } get { return (string)GetValue(JSharpContentProperty); } }

        //C#语言内容
        public static ExtendProperty CSharpContentProperty = RegisterProperty<MainViewModel>(v => v.CSharpContent);
        public string CSharpContent { set { SetValue(CSharpContentProperty, value); } get { return (string)GetValue(CSharpContentProperty); } }

        public static ExtendProperty OpenContentProperty = RegisterProperty<MainViewModel>(v => v.OpenContent);
        public string OpenContent { set { SetValue(OpenContentProperty, value); } get { return (string)GetValue(OpenContentProperty); } }

        private Session session;


        public override string GetViewTitle()
        {
            return "CSharp to Javascript Compiler base on Roslyn";
        }

        //编译运行
        public void MakeAndRun()
        {
            var mv = this.View as IMainView;
            //mv.Formatter.Clear();
            try
            {
                var scriptEngine = InteractiveManager.GetScriptEngine();
                session = scriptEngine.CreateSession();
                session.Execute(this.CSharpContent);
                mv.Formatter.WriteObject("Make all success." + DateTime.Now.ToString());
            }
            catch (Exception ex)
            {
                mv.Formatter.WriteError(ex);
            }
        }

        //构建Javascript
        public void Build()
        {
            var tree = SyntaxTree.ParseText(CSharpContent);
            MetadataReference mscorlib = MetadataReference.CreateAssemblyReference("mscorlib");
            var root = (CompilationUnitSyntax)tree.GetRoot();
            var compilation = Compilation.Create("MyCompilation")
                .AddReferences(mscorlib)
                .AddSyntaxTrees(tree);
            var model = compilation.GetSemanticModel(tree);
           

            var invocationSyntax = root.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .ToList();

          

            //var symbolInfo = model.GetSymbolInfo(invocationSyntax);
            //var methodSymbol = (MethodSymbol)symbolInfo.Symbol;

            //foreach (MethodSymbol overload in methodSymbol.ContainingType.GetMembers(methodSymbol.Name))
            //{
            //    // also look at overload.Parameters
            //    Console.WriteLine(overload);
            //}

            //string js = JavaScriptCompiler.EmitJs(this.CSharpContent);

            //this.JSharpContent = js;
            //#region debug
        
            //string CompiledScriptClass = "Submission#0";
            //string CompiledScriptMethod = "<Factory>";
            //string path = "e:\\debug";
            //string outputPath = "temp.dll";
            //string pdbPath = "temp.pdb";


            //var scriptEngine = new ScriptEngine();
            //scriptEngine.AddReference("System");
            //scriptEngine.AddReference("System.Core");
            //var session = scriptEngine.CreateSession();
            
            //Submission<object> submission = session.CompileSubmission<object>(CSharpContent);

            //var exeBytes = new byte[0];
            //var pdbBytes = new byte[0];
            //var compileSuccess = false;

            //using (var exeStream = new MemoryStream())
            //using (var pdbStream = new MemoryStream())
            //{
            //    var result = submission.Compilation.Emit(exeStream, pdbStream: pdbStream);
            //    compileSuccess = result.Success;

            //    if (result.Success)
            //    {
                 
            //        exeBytes = exeStream.ToArray();
            //        pdbBytes = pdbStream.ToArray();
            //    }
            //    else
            //    {
            //        var errors = String.Join(Environment.NewLine, result.Diagnostics.Select(x => x.ToString()));
                  
            //    }
            //}

            //if (compileSuccess)
            //{
            //    Console.WriteLine("Compilation successful");
            //    Console.WriteLine(string.Format("Output .dll at {0}", outputPath));
            //    Console.WriteLine(string.Format("Output .pdb at {0}", pdbPath));

            //    try
            //    {
            //        Assembly assembly = AppDomain.CurrentDomain.Load(exeBytes, pdbBytes);

            //        var type = assembly.GetType(CompiledScriptClass);
                
            //        var method = type.GetMethod(CompiledScriptMethod, BindingFlags.Static | BindingFlags.Public);

            //        method.Invoke(null, new[] { session });
            //    }
            //    catch (Exception ex)
            //    {
            //        MessageBox.Show(ex.Message);
            //    }
            //}

            //#endregion
        }
     
        public void Open()
        {
            OpenFileDialog odf = new OpenFileDialog();
            odf.Filter = "csharp files (*.cs)|*.cs|All files (*.*)|*.*";
            if (odf.ShowDialog().Value)
            {
                string fileName = odf.FileName;
                string content = File.ReadAllText(fileName, Encoding.UTF8);


                OpenContent = content;

            }
        }




    }
}
