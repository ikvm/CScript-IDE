using System;
using System.Collections.Generic;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;

namespace jinx.RoslynEditor
{
	/// <summary>
	/// Allows producing foldings from a document based on braces.
	/// </summary>
	public class BraceFoldingStrategy : AbstractFoldingStrategy
	{
		/// <summary>
		/// Gets/Sets the opening brace. The default value is '{'.
		/// </summary>
		public char OpeningBrace { get; set; }
		
		/// <summary>
		/// Gets/Sets the closing brace. The default value is '}'.
		/// </summary>
		public char ClosingBrace { get; set; }


        public string openingBraceStr = "#region";
        public string closingBraceStr = "#endregion";

        private int firstUsing = -1 ,lastUsing =  -1;
        private DocumentLine firstUsingline, lastUsingline;

		/// <summary>
		/// Creates a new BraceFoldingStrategy.
		/// </summary>
		public BraceFoldingStrategy()
		{
			this.OpeningBrace = '{';
			this.ClosingBrace = '}';
		}
		
		/// <summary>
		/// Create <see cref="NewFolding"/>s for the specified document.
		/// </summary>
		public override IEnumerable<NewFolding> CreateNewFoldings(TextDocument document, out int firstErrorOffset)
		{
			firstErrorOffset = -1;
			return CreateNewFoldings(document);
		}
		
		/// <summary>
		/// Create <see cref="NewFolding"/>s for the specified document.
		/// </summary>
		public IEnumerable<NewFolding> CreateNewFoldings(ITextSource document)
		{
			List<NewFolding> newFoldings = new List<NewFolding>();
			
			Stack<int> startOffsets = new Stack<int>();
			int lastNewLineOffset = 0;
			char openingBrace = this.OpeningBrace;
			char closingBrace = this.ClosingBrace;

			for (int i = 0; i < document.TextLength; i++) {
				char c = document.GetCharAt(i);
				if (c == openingBrace) {
					startOffsets.Push(i);
				} else if (c == closingBrace && startOffsets.Count > 0) {
					int startOffset = startOffsets.Pop();
					// don't fold if opening and closing brace are on the same line
					if (startOffset < lastNewLineOffset) {
						newFoldings.Add(new NewFolding(startOffset, i + 1));
					}
				}

                int slen = document.Text.Length < openingBraceStr.Length + i ? 1 : openingBraceStr.Length;
                string st = document.GetText(i, slen).ToLower();
                int elen = document.Text.Length < closingBraceStr.Length + i ? 1 : closingBraceStr.Length;
                string et = document.GetText(i, elen).ToLower();
                if (st == openingBraceStr)
                {

                    startOffsets.Push(i);
                }
                else if (et == closingBraceStr && startOffsets.Count > 0)
                {
                    int startOffset = startOffsets.Pop();
                    // don't fold if opening and closing brace are on the same line
                    if (startOffset < lastNewLineOffset)
                    {
                        var textDocument = (TextDocument)document;
                        int regionOffset = startOffset + slen;
                        var line = textDocument.GetLineByOffset(regionOffset);
                        if (regionOffset < line.EndOffset)
                        {
                            int regionToLineEndOffset = line.EndOffset - regionOffset;
                            string foldingName = document.GetText(startOffset + slen, regionToLineEndOffset);
                            var folding = new NewFolding(startOffset, i + elen);
                            folding.Name = foldingName;
                            newFoldings.Add(folding);
                        }
                    }
                }
               
                var doc = (TextDocument)document;
                var sline = doc.GetLineByOffset(i);
                string lineStr = doc.GetText(sline.Offset,sline.Length);
                if (lineStr.StartsWith("using") || lineStr.StartsWith("#r"))
                {
                    if (firstUsing == -1)
                    {
                        firstUsing = sline.LineNumber;
                        firstUsingline = sline;
                    }
                    else if (lastUsing != firstUsing)
                    {
                        lastNewLineOffset = sline.LineNumber;
                        lastUsingline = sline;
                    }


                }
                else
                {
                    if (firstUsing != -1 && firstUsingline.Offset < lastUsingline.EndOffset)
                    {
                        
                        var folding = new NewFolding(firstUsingline.Offset, lastUsingline.EndOffset);
                        folding.Name = "using";
                        newFoldings.Add(folding);
                    }
                }
              
                



                if (c == '\n' || c == '\r')
                {
                    lastNewLineOffset = i + 1;
                }



			}

      
			newFoldings.Sort((a,b) => a.StartOffset.CompareTo(b.StartOffset));
			return newFoldings;
		}
	}
}
