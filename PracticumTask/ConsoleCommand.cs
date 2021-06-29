


using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;



namespace ConsoleApplicationBase
{
    public class ConsoleCommand
    {
        public ConsoleCommand(string input)
        {
            // split string on spaces, but preserve quoted text
            string[] inputArray = Regex.Split(input, "(?<=^[^\"]*(?:\"[^\"]*\"[^\"]*)*) (?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
            _arguments = new List<string>();
            
            string[] namesArray = inputArray[0].Split('.');
            if (namesArray.Length == 2)
            {
                Name = namesArray[1];
                LibraryClassName = namesArray[0];
            }
            else
            {
                Name = inputArray[0];
                LibraryClassName = "DefaultCommands";
            }

            for (int i = 1; i < inputArray.Length; i++)
            {
                string inputArgument = inputArray[i];
                
                // check if text is quoted
                Regex regex = new Regex("\"(.*?)\"", RegexOptions.Singleline);
                Match match = regex.Match(inputArgument);
                
                if (match.Captures.Count > 0)
                {
                    // get the unquoted text
                    Regex captureQuotedText = new Regex("[^\"]*[^\"]");
                    Match quoted = captureQuotedText.Match(match.Captures[0].Value);

                    _arguments.Add(quoted.Captures[0].Value);
                }
                else
                {
                    _arguments.Add(inputArgument);
                }
                
            }
        }

        public string Name { get; set; }
        public string LibraryClassName { get; set; }

        private List<string> _arguments;
        public IEnumerable<string> Arguments
        {
            get
            {
                return _arguments;
            }
        }
    }

}