

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.IO;
using System.Text;
using System.Web;
using HtmlAgilityPack;

namespace ConsoleApplicationBase.Commands
{
    public static class DefaultCommands
    {
        public static string CountWords(string url, string outputFilePath = null)
        {
            HttpClient client = new HttpClient();
            char[] separators = {'\\', '/', ' ', ',', '.', '!', '?', '"', '\'', ';', ':', '[', ']', '(', ')', '\n', '\r', '\t' };
            string htmlText = "";
            
            // try to load the html page
            try
            {
                HttpResponseMessage response = client.GetAsync(url).Result;
                response.EnsureSuccessStatusCode();
                htmlText = response.Content.ReadAsStringAsync().Result;
            }
            catch (HttpRequestException e)
            {
                return e.Message;
            }

            if (!string.IsNullOrWhiteSpace(outputFilePath))
            {
                using (StreamWriter file = new StreamWriter(outputFilePath))
                {
                    file.Write(htmlText);
                }
            }

            // extract viewable text, decode special characters
            HtmlDocument html = new HtmlDocument();
            html.LoadHtml(htmlText);
            htmlText = ExtractViewableText(html.DocumentNode);
            htmlText = HttpUtility.HtmlDecode(htmlText);

            // split into words and count unique words
            string[] words = htmlText.Split(separators);
            SortedDictionary<string, uint> wordCounters = new SortedDictionary<string, uint>();

            foreach (string word in words)
            {
                if (string.IsNullOrWhiteSpace(word))
                {
                    continue;
                }

                string loweredWord = word.ToLower();

                if (wordCounters.ContainsKey(loweredWord))
                {
                    wordCounters[loweredWord]++;
                }
                else
                {
                    wordCounters[loweredWord] = 1;
                }
            }
            
            string output = "";
            foreach (KeyValuePair<string, uint> wordCountPair in wordCounters)
            {
                output += wordCountPair.Key + " - " + wordCountPair.Value + "\n";
            }

            return output;
        }

        private static string ExtractViewableText(HtmlNode node)
        {
            StringBuilder sb = new StringBuilder();
            ExtractViewableText(sb, node);
            return sb.ToString();
        }

        private static void ExtractViewableText(StringBuilder sb, HtmlNode node)
        {
            if (node.Name == "script" || node.Name == "style")
            {
                return;
            }

            if (node.NodeType == HtmlNodeType.Text)
            {
                AppendNodeText(sb, node);
            }

            foreach (HtmlNode child in node.ChildNodes)
            {
                ExtractViewableText(sb, child);
            }
        }

        private static void AppendNodeText(StringBuilder sb, HtmlNode node)
        {
            string text = ((HtmlTextNode)node).Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            sb.Append(text);

            // add space at the end if there is none
            if (!(text.EndsWith("\t") || text.EndsWith("\n") || text.EndsWith(" ") || text.EndsWith("\r")))
            {
                sb.Append(" ");
            }
        }
    }
}