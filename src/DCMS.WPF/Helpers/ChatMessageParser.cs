using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace DCMS.WPF.Helpers
{
    public static class ChatMessageParser
    {
        private static readonly Regex RecordRegex = new Regex(@"(@(IN|OUT)-[A-Za-z0-9\-]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex UserRegex = new Regex(@"(@\w+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static IEnumerable<Inline> ParseMessage(string message, Action<string> onRecordClicked, Action<string> onUserMentionClicked = null)
        {
            var inlines = new List<Inline>();
            if (string.IsNullOrEmpty(message)) return inlines;

            var matches = new List<(int Index, int Length, string Value, bool IsRecord)>();
            
            foreach (Match m in RecordRegex.Matches(message))
                matches.Add((m.Index, m.Length, m.Value, true));
            
            foreach (Match m in UserRegex.Matches(message))
            {
                if (!matches.Any(prev => m.Index >= prev.Index && m.Index < prev.Index + prev.Length))
                    matches.Add((m.Index, m.Length, m.Value, false));
            }

            var orderedMatches = matches.OrderBy(m => m.Index).ToList();
            int lastIndex = 0;

            foreach (var match in orderedMatches)
            {
                if (match.Index > lastIndex)
                {
                    inlines.Add(new Run(message.Substring(lastIndex, match.Index - lastIndex)));
                }

                var code = match.Value;
                var cleanValue = code.Substring(1);

                var link = new Hyperlink(new Run(code))
                {
                    Foreground = match.IsRecord ? Brushes.DeepSkyBlue : Brushes.ForestGreen,
                    FontWeight = FontWeights.Bold,
                    TextDecorations = null,
                    Cursor = System.Windows.Input.Cursors.Hand,
                    ToolTip = match.IsRecord ? "فتح المراسلة" : "بدء محادثة مع المستخدم"
                };

                if (match.IsRecord)
                    link.Click += (s, e) => onRecordClicked?.Invoke(cleanValue);
                else
                    link.Click += (s, e) => onUserMentionClicked?.Invoke(cleanValue);

                inlines.Add(link);
                lastIndex = match.Index + match.Length;
            }

            if (lastIndex < message.Length)
            {
                inlines.Add(new Run(message.Substring(lastIndex)));
            }

            return inlines;
        }

        // Backward compatibility for existing calls
        public static IEnumerable<Inline> ParseMessage(string message, Action<string> onMentionClicked)
        {
            return ParseMessage(message, onMentionClicked, null);
        }
    }
}
