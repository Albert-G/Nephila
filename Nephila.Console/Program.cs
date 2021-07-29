using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Nephila
{
    class Program
    {
        private static readonly string _prompt = "Nephila>";
        private static Nephila _nephilaInstance = null;

        static void Main(string[] args)
        {
            var path = Path.GetFullPath(args.Length > 0 ? args[0].Replace("\"", "") : "./ ");

            Console.Write($"Processing Assemblies under {path} ... ");
            _nephilaInstance = new Nephila(new ConsoleLogger(), path);

            Console.WriteLine("Done");
            ReadLine.AutoCompletionHandler = new AutoCompletionHandler();

            while (true)
            {
                var input = ReadLine.Read(_prompt);
                if (input.Equals("exit", StringComparison.CurrentCultureIgnoreCase))
                {
                    break;
                }

                var assemblyRefPairs = _nephilaInstance.GetReferencePairs(input);
                if (assemblyRefPairs.Count <= 0)
                {
                    Console.WriteLine($"No result for {input}.");
                    continue;
                }

                DrawAssemblyReferenceDiagram(assemblyRefPairs, input);
            }
        }

        private static void DrawAssemblyReferenceDiagram(HashSet<Tuple<Nephila.AssemblyReference, Nephila.AssemblyReference>> assemblyRefPairs, string input)
        {
            var form = new System.Windows.Forms.Form();
            form.Text = input;

            var gviewer = new Microsoft.Msagl.GraphViewerGdi.GViewer();
            var graph = new Microsoft.Msagl.Drawing.Graph(input);

            //create assembly ref diagram
            foreach (var arp in assemblyRefPairs)
            {
                graph.AddEdge(arp.Item1.String, arp.Item2.String);
            }

            gviewer.Graph = graph;
            form.SuspendLayout();
            gviewer.Dock = System.Windows.Forms.DockStyle.Fill;
            form.Controls.Add(gviewer);
            form.ResumeLayout();
            form.ShowDialog();
        }

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
