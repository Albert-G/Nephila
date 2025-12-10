using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace Nephila
{
    public class AssemblyReference
    {
        public string FileName { get; set; }
        public Version Version { get; set; }
        public Assembly Assembly { get; set; }
        public bool FileLoaded
        {
            get
            {
                return Assembly != null;
            }
        }
        public ConcurrentDictionary<string, AssemblyReference> ReferredBy { get; set; } = new ConcurrentDictionary<string, AssemblyReference>();

        public override string ToString()
        {
            return String;
        }

        private string _string;
        public string String
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_string))
                {
                    _string = $"{FileName} ({Version}){(FileLoaded ? string.Empty : " (Unloaded)")}";
                }
                return _string;
            }
        }
    }
}
