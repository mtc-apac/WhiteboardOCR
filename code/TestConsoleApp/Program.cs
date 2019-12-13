﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web;

namespace TestConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var text1 = @"my Sample Notes
# Background #
This is about so and so ...
And this ; and that
# Highlights #
> point
> dot
> triangle
# Next Steps #
1. *PersonA* to do this
2. *PersonB* to do that
3 *PersonC* will do something else
4 . *PersonD* will do nothing

1. No1
2 No2
> Bullet1
3 .No3
> Bullet2
> Bullet3
4.Bullet4
5. Bullet5

#*!bold and italicized header! -- bold header * -- header#
*!#just*!#wrong";
            Console.WriteLine("###################################################################");
            Console.WriteLine("-- RAW Text--------------------------------------------------------");
            Console.WriteLine("-------------------------------------------------------------------");
            Console.WriteLine(text1);
            Console.WriteLine();
            Console.WriteLine("-- OUTPUT Text-----------------------------------------------------");
            Console.WriteLine();
            Console.WriteLine(ConvertMarkdownTextToOneNoteHtml(text1, "2019-12-12"));
            Console.WriteLine("-------------------------------------------------------------------");
            Console.WriteLine();

            Console.ReadKey();
        }

        private static string ConvertMarkdownTextToOneNoteHtml(string text, string dateTimeString)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            var lines = text.Split('\n');
            var outputLines = new List<string>();

            var orderedStartIndex = -1;
            var orderedLastNumber = 0;

            var unorderedStartIndex = -1;
            var unorderedCount = 0;

            outputLines.Add("<html>");
            outputLines.Add("<head>");
            outputLines.Add(string.Concat("<title>", lines[0].Trim().Replace("\r", string.Empty), "</title>"));
            if (!string.IsNullOrEmpty(dateTimeString))
                outputLines.Add(string.Concat("<meta name='created' content='", dateTimeString, "' />"));
            outputLines.Add("</head>");

            outputLines.Add("<body>");
            for (var i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim().Replace("\r", string.Empty);

                // Process Ordered/Unordered Lists
                var listType = DetectListType(line);
                if (listType.HasValue) // Start List
                {
                    if (listType.Value == ListType.Ordered)
                    {
                        if (unorderedStartIndex != -1)
                        {
                            if (unorderedCount > 1)
                                ProcessList(outputLines, unorderedStartIndex, outputLines.Count - 1, false);
                            unorderedStartIndex = -1;
                            unorderedCount = 0;
                        }

                        var num = int.Parse(line[0].ToString());
                        if (num == 1)
                        {
                            if (orderedLastNumber > 1)
                                ProcessList(outputLines, orderedStartIndex, outputLines.Count - 1, true);

                            orderedStartIndex = outputLines.Count;
                            orderedLastNumber = 1;
                        }
                        else
                        {
                            
                            if (orderedLastNumber + 1 == num)
                                orderedLastNumber = num;
                            else
                            {
                                if (orderedLastNumber > 1)
                                    ProcessList(outputLines, orderedStartIndex, outputLines.Count - 1, true);

                                orderedStartIndex = -1;
                                orderedLastNumber = 0;
                            }
                        }
                    }
                    else if (listType.Value == ListType.Unordered)
                    {
                        if (orderedStartIndex != -1)
                        {
                            if (orderedLastNumber > 1)
                                ProcessList(outputLines, orderedStartIndex, outputLines.Count - 1, true);
                            orderedStartIndex = -1;
                            orderedLastNumber = 0;
                        }

                        if (unorderedStartIndex == -1)
                            unorderedStartIndex = outputLines.Count;
                        
                        unorderedCount++;
                    }
                }
                else // End existing list, if any
                {
                    if (orderedStartIndex != -1)
                    {
                        if (orderedLastNumber > 1)
                            ProcessList(outputLines, orderedStartIndex, outputLines.Count - 1, true);
                        orderedStartIndex = -1;
                        orderedLastNumber = 0;
                    }
                    else if (unorderedStartIndex != -1)
                    {
                        if (unorderedCount > 1)
                            ProcessList(outputLines, unorderedStartIndex, outputLines.Count - 1, false);
                        unorderedStartIndex = -1;
                        unorderedCount = 0;
                    }
                }

                // Process Markdowns
                line = ReplaceMarkdownEnclosure(line, '#', "<h1>", "</h1>");
                line = ReplaceMarkdownEnclosure(line, '*', "<b>", "</b>");
                line = ReplaceMarkdownEnclosure(line, '!', "<i>", "</i>");

                outputLines.Add(line.Trim());
            }

            if (orderedStartIndex != -1 && orderedLastNumber > 1)
                ProcessList(outputLines, orderedStartIndex, outputLines.Count - 1, true);
            else if (unorderedStartIndex != -1 && unorderedCount > 1)
                ProcessList(outputLines, unorderedStartIndex, outputLines.Count - 1, false);

            outputLines.Add("</body>");
            outputLines.Add("</html>");

            return string.Join('\n', outputLines);
        }

        private enum MarkdownType
        {
            Header, Bold, Italic
        }

        private enum ListType
        {
            Ordered, Unordered
        }

        private static MarkdownType? DetectMarkdownCharacter(char c)
        {
            switch (c)
            {
                case '#': return MarkdownType.Header;
                case '*': return MarkdownType.Bold;
                case '!': return MarkdownType.Italic;
                default: return null;
            }
        }

        private static ListType? DetectListType(string line)
        {
            if (line.Length == 0)
                return null;

            var c = line[0];
            switch (c)
            {
                case '>': return ListType.Unordered;
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9': return ListType.Ordered;
                default: return null;
            }
        }

        private static bool ProcessList(List<string> lines, int startIndex, int endIndex, bool isOrdered)
        {
            if (lines.Count == 0 || startIndex < 0 || endIndex < 0)
                return false;

            if (endIndex < startIndex)
                endIndex = lines.Count - 1;

            for (var i = startIndex; i <= endIndex; i++)
            {
                var line = lines[i];
                
                line = line.Substring(1);
                if (line.StartsWith(" ."))
                    line = line.Substring(2);
                else if (line.StartsWith("."))
                    line = line.Substring(1);

                lines[i] = string.Concat("<li>", line.Trim(), "</li>");
            }

            if (endIndex + 1 == lines.Count)
                lines.Add(isOrdered ? "</ol>" : "</ul>");
            else
                lines.Insert(endIndex + 1, isOrdered ? "</ol>" : "</ul>");

            lines.Insert(startIndex, isOrdered ? "<ol>" : "<ul>");

            return true;
        }

        private static string ReplaceMarkdownEnclosure(string line, char markdown, string startHtmlTag, string endHtmlTag)
        {
            var count = line.Count(c => c == markdown);
            
            if (count < 2)
                return line;

            var markdownIndex = line.IndexOf(markdown);
            var output = line.Substring(0, markdownIndex);
            output = output + startHtmlTag;

            line = line.Substring(markdownIndex + 1);
            markdownIndex = line.IndexOf(markdown);

            if (markdownIndex > 0)
                output = output + line.Substring(0, markdownIndex);
            output = output + endHtmlTag;
            if (markdownIndex + 1 < line.Length)
            {
                line = line.Substring(markdownIndex + 1);
                output = output + line;
            }

            if (count == 2)
                return output;

            return ReplaceMarkdownEnclosure(output, markdown, startHtmlTag, endHtmlTag);
        }
    }
}
