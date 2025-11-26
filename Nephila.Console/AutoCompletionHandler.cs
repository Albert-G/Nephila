using System;
using System.Linq;

namespace Nephila
{
    partial class Program
    {
        class AutoCompletionHandler : IAutoCompleteHandler
        {
            public char[] Separators { get; set; } = new char[] { ' ', '/' };

            public string[] GetSuggestions(string text, int index)
            {
                return _nephilaInstance.GetAssemblyNames(text).ToArray();
            }
        }
    }
}
