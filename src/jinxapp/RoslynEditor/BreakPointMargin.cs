using ICSharpCode.AvalonEdit.Editing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace jinxapp.RoslynEditor
{
    public class BreakPointMargin : AbstractMargin
    {
        private const int margin = 20;
        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            return new PointHitTestResult(this, hitTestParameters.HitPoint);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size(margin, 0);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            Size renderSize = this.RenderSize;
            var textView = this.TextView;

            drawingContext.DrawRectangle(Brushes.LightGray, null,
                new Rect(0, 0, 22.0, renderSize.Height));
            if (textView != null && textView.VisualLinesValid)
            {
                foreach (var line in textView.VisualLines)
                {
                    drawingContext.DrawEllipse(Brushes.Red, null, new Point((renderSize.Width - 22) / 2 + 10,(line.VisualTop - textView.VerticalOffset)/2 + 10) , 10, 10);
                   
      
                }
            }
            base.OnRender(drawingContext);
        }

 

    }

}
