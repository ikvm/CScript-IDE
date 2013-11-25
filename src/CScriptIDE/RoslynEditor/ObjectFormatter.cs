using CScriptIDE.RoslynEditor.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;


namespace CScriptIDE.RoslynEditor
{
    public class ObjectFormatter
    {
        private readonly FlowDocument _document;

        public ObjectFormatter(FlowDocument document)
        {
            _document = document;
        }

        public void Clear()
        {
            _document.Blocks.Clear();
        }

        public void WriteObject(object o)
        {
            var objects = new HashSet<object>(new ReferenceEqualityComparer());
            WriteObject(_document.Blocks, objects, o);
        }

        public void WriteError(Exception exception)
        {
            WriteError(_document.Blocks, exception);
        }

        public static void WriteError(BlockCollection blockCollection, Exception ex)
        {
            blockCollection.Add(CreatePara(new Run(ex.Message) { Foreground = Brushes.Red }));
        }

        private static void WriteObject(BlockCollection blocks, HashSet<object> objects, object o)
        {
            if (o == null)
            {
                blocks.Add(CreatePara(new Run("<null>")));
                return;
            }

            if (!(o is string))
            {
                var enumerable = o as IEnumerable;
                if (enumerable != null)
                {
                    WriteEnumerable(blocks, objects, enumerable);
                    return;
                }
            }

            if(o is UIElement)
            {
                blocks.Add(CreatePara(new Figure( new BlockUIContainer((UIElement)o) )));
                return;
            }



            WriteProperties(blocks, objects, o);
        }

        private static void WriteEnumerable(BlockCollection blocks, HashSet<object> objects, IEnumerable enumerable)
        {
            var list = new List();
            foreach (var item in enumerable)
            {
                var listItem = new ListItem();
                WriteProperties(listItem.Blocks, objects, item);
                list.ListItems.Add(listItem);
            }
            blocks.Add(list);
        }

        private static void WriteProperties(BlockCollection blocks, HashSet<object> objects, object o)
        {
            if (!objects.Add(o))
            {
                return;
            }
            var type = o.GetType();
            if (type.IsValueType || type == typeof(string))
            {
                blocks.Add(CreatePara(new Run(o.ToString())));
                return;
            }

            var rowGroup = new TableRowGroup();
            foreach (var propertyInfo in type.GetProperties())
            {
                if (propertyInfo.CanRead && propertyInfo.GetIndexParameters().Length == 0)
                {
                    var row = new TableRow();
                    row.Cells.Add(new TableCell(CreatePara(new Run(propertyInfo.Name))));
                    var valueCell = new TableCell();
                    object propertyValue;
                    try
                    {
                        propertyValue = propertyInfo.GetValue(o, null);
                    }
                    catch (Exception ex)
                    {
                        propertyValue = null;
                        WriteError(valueCell.Blocks, ex);
                    }
                    if (propertyValue != null)
                    {
                        WriteObject(valueCell.Blocks, objects, propertyValue);
                    }
                    row.Cells.Add(valueCell);
                    rowGroup.Rows.Add(row);
                }
            }
            var table = new Table { BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1) };
            table.Columns.Add(new TableColumn { Width = GridLength.Auto });
            table.Columns.Add(new TableColumn { Width = GridLength.Auto });
            table.RowGroups.Add(rowGroup);
            blocks.Add(table);
        }

        private static Paragraph CreatePara(Inline inline = null)
        {
            var para = new Paragraph { TextAlignment = TextAlignment.Left };
            if (inline != null)
            {
                para.Inlines.Add(inline);
            }
            return para;
        }
    }
}