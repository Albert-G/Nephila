using System.Text;

namespace Nephila
{
    public static class NepilaMermaidExtension
    {
        public static string NepilaMermaidOutput(this Nephila nephila, string assemblyFileName)
        {
            var pairs = nephila.GetReferencePairs(assemblyFileName);

            StringBuilder sb = new StringBuilder();
            foreach (var pair in pairs)
            {
                string assembly = pair.Item1.String;
                string refferedBy = pair.Item2.String;

                sb.AppendLine($"{assembly.GetHashCode()}[\"{assembly}\"] --> {refferedBy.GetHashCode()}[\"{refferedBy}\"]");
                //sb.AppendLine($"\"{assembly}\" -> \"{refferedBy}\"");
            }

            return sb.ToString();
        }
    }
}
