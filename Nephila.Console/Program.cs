using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Microsoft.Msagl.GraphViewerGdi;

namespace Nephila
{
    partial class Program
    {
        private static readonly string _prompt = "Nephila>";
        private static Nephila _nephilaInstance = null;

        private static Form _form = null;
        private static GViewer _gviewer = null;

        [STAThread]
        static void Main(string[] args)
        {
            GraphInit();

            string path = string.Empty;
            if (args.Length > 0)
            {
                path = Path.GetFullPath(args.Length > 0 ? args[0].Replace("\"", "") : "./ ");
            }
            else
            {
                // Open folder selection dialog when no path argument is provided
                using (var dialog = new FolderBrowserDialog())
                {
                    dialog.Description = "Select assembly folder";
                    dialog.UseDescriptionForTitle = true;
                    dialog.ShowNewFolderButton = false;

                    var result = dialog.ShowDialog();
                    if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
                    {
                        path = Path.GetFullPath(dialog.SelectedPath);
                    }
                    else
                    {
                        path = Path.GetFullPath("./");
                    }
                }
            }

            var recusiveDepth = args.Length > 1 && int.TryParse(args[1], out var depth) ? depth : 3;

            Console.Write($"Processing Assemblies under {path}, recusive depth {recusiveDepth}... \n");
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

                var assemblyRefPairs = _nephilaInstance.GetReferencePairs(input, 3);
                if (assemblyRefPairs.Count <= 0)
                {
                    Console.WriteLine($"No result for {input}.");
                    continue;
                }

                DrawAssemblyReferenceDiagram(assemblyRefPairs, input);
            }
        }

        private static void GraphInit()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            _form = new Form
            {
                WindowState = FormWindowState.Maximized,
            };

            _gviewer = new GViewer
            {
                Dock = DockStyle.Fill,
            };

            _form.Controls.Add(_gviewer);
        }

        private static void DrawAssemblyReferenceDiagram(HashSet<Tuple<AssemblyReference, AssemblyReference>> assemblyRefPairs, string assemblyName)
        {
            _form.Text = assemblyName;

            var graph = new Microsoft.Msagl.Drawing.Graph(assemblyName);

            //create assembly ref diagram
            foreach (var arp in assemblyRefPairs)
            {
                graph.AddEdge(arp.Item1.String, arp.Item2.String);
            }

            _gviewer.Graph = graph;
            _form.ShowDialog();
        }
    }
}
