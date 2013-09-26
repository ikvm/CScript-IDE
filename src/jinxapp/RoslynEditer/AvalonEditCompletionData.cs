using System;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using Roslyn.Compilers;
using Roslyn.Services;
using System.Reflection;
using System.Windows.Media.Imaging;
using jinxapp.RoslynEditer;

namespace RoslynPad.Editor
{
    public class AvalonEditCompletionData : ICompletionData
    {
        private readonly CompletionItem _item;
        private CompletionDescription _description;

        public AvalonEditCompletionData(CompletionItem item)
        {
            
            _item = item;
            Text = item.DisplayText;
            Content = Text;
            if (item.Glyph != null)
            {
                Image = GlyphToImage(item.Glyph.Value);
                if (item.Glyph.Value == Glyph.Keyword)
                {
                    this.Priority = 999.99;
                }
            }
            
        }

       private ImageSource GlyphToImage(Glyph glyph)
       {
           string imageName = string.Empty;
           switch(glyph)
           {
               case Glyph.Assembly:
                   imageName = "Assembly";
                   break;
               case Glyph.ClassInternal:
               case Glyph.ClassPrivate:
               case Glyph.ClassProtected:
               case Glyph.ClassPublic:
                   imageName = "Class";
                   break;
               case Glyph.EventInternal:
               case Glyph.EventPrivate:
               case Glyph.EventProtected:
               case Glyph.EventPublic:
                   imageName = "Event";
                   break;
               case Glyph.FieldInternal:
               case Glyph.FieldPrivate:
               case Glyph.FieldProtected:
               case Glyph.FieldPublic:
                   imageName = "Field";    
                   break;
               case Glyph.InterfaceInternal:
               case Glyph.InterfacePrivate:
               case Glyph.InterfaceProtected:
               case Glyph.InterfacePublic:
                   imageName = "Interface";
                   break;
               case Glyph.MethodInternal:
               case Glyph.MethodPrivate:
               case Glyph.MethodProtected:
               case Glyph.MethodPublic:
                   imageName = "Method";
                   break;
               case Glyph.PropertyInternal:
               case Glyph.PropertyPrivate:
               case Glyph.PropertyProtected:
               case Glyph.PropertyPublic:
                   imageName = "Property";
                   break;
               case Glyph.Namespace:
                   imageName = "Namespace";
                   break;
               case Glyph.EnumPublic:
                   imageName = "Enum";
                   break;
               case Glyph.Keyword:
                   imageName = "Keyword";
                   break;
               case Glyph.ExtensionMethodPublic:
               case Glyph.ExtensionMethodProtected:
               case Glyph.ExtensionMethodPrivate:
               case Glyph.ExtensionMethodInternal:
                   imageName = "ExtensionMethod";
                   break;
               default:
                   imageName = "Type";
                   break;

           }

           return new BitmapImage(new Uri(@"pack://application:,,,/" + Assembly.GetExecutingAssembly().GetName().Name + ";component/" + "Images/" + imageName + ".png", UriKind.Absolute));

       }


        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs e)
        {
            textArea.Document.Replace(completionSegment.Offset - 1, completionSegment.Length + 1, Text);
        }

        public ImageSource Image { get; private set; }
        public string Text { get; private set; }
        public object Content
        {
            get;
            private set;
            //get
            //{

            //    _description = new CompletionDescription();

            //    _description.DataContext = this.Text;
            //    return _description;
            //}
        }
        public object Description
        {
            get
            {
                var desciption = _item.GetDescription();
                string descriptionContent =  desciption.ToDisplayString();
                
    
                _description = new CompletionDescription();
            
                _description.DataContext = descriptionContent;


                return _description;
            }
        }

        public double Priority { get; private set; }
    }
}