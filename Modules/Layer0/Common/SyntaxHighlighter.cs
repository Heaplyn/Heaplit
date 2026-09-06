// Developer: heaplyn
// Date: 2026-08-18
// Summary: High-performance syntax highlighter for WPF RichTextBox.
//          Fixes the "broken words" issue by using a robust tokenization method
//          and avoiding offset drift caused by document tags.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Linq;

namespace HeaplitLauncher
{
    public static class SyntaxHighlighter
    {
        public static void Highlight(RichTextBox rtb, string extension)
        {
            if (!EditorIntelligenceManager.SyntaxHighlightingRules.TryGetValue(extension, out var rules)) return;

            var document = rtb.Document;
            var totalRange = new TextRange(document.ContentStart, document.ContentEnd);
            string text = totalRange.Text;

            // Normalize line endings for regex consistency
            string normalizedText = text.Replace("\r\n", "\n");
            if (normalizedText.Length > 100000) return;

            rtb.BeginChange();
            try {
                // 1. Reset all formatting to default base state
                totalRange.ClearAllProperties();
                totalRange.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.White);
                totalRange.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Normal);

                // 2. Map matches using indices on the normalized text
                var matches = new List<(int Index, int Length, SyntaxRule Rule)>();
                foreach (var rule in rules) {
                    // Use word boundaries for keywords/registers to avoid partial matches (e.g., 'di' in 'Pointer')
                    foreach (Match m in Regex.Matches(normalizedText, rule.Pattern, RegexOptions.Compiled | RegexOptions.Multiline)) {
                        matches.Add((m.Index, m.Length, rule));
                    }
                }

                // 3. Apply matches in forward order using a reliable offset mapper
                var sorted = matches.OrderBy(m => m.Index).ToList();
                TextPointer startPos = document.ContentStart;

                foreach (var m in sorted) {
                    TextPointer p1 = GetPointAtOffset(startPos, m.Index);
                    TextPointer p2 = GetPointAtOffset(p1, m.Length);

                    if (p1 != null && p2 != null) {
                        var range = new TextRange(p1, p2);
                        try {
                            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(m.Rule.ColorHex));
                            range.ApplyPropertyValue(TextElement.ForegroundProperty, brush);
                            if (m.Rule.IsBold) range.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
                        } catch { }
                    }
                }
            } finally { rtb.EndChange(); }
        }

        private static TextPointer GetPointAtOffset(TextPointer start, int offset)
        {
            TextPointer p = start;
            int count = 0;

            while (p != null && count < offset)
            {
                var context = p.GetPointerContext(LogicalDirection.Forward);
                if (context == TextPointerContext.Text)
                {
                    int runLength = p.GetTextInRun(LogicalDirection.Forward).Length;
                    if (count + runLength >= offset)
                    {
                        return p.GetPositionAtOffset(offset - count);
                    }
                    count += runLength;
                }
                else if (context == TextPointerContext.ElementStart || context == TextPointerContext.ElementEnd)
                {
                    // Symbols like paragraph tags or runs don't count towards the character offset in normalized text
                }

                TextPointer next = p.GetNextContextPosition(LogicalDirection.Forward);
                if (next == null || next.CompareTo(p) == 0) break;
                p = next;
            }
            return p;
        }
    }
}
