using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Nephila
{
    public class Nephila
    { 
        private readonly ConcurrentDictionary<string, AssemblyReference> _assemblyIndex = new ConcurrentDictionary<string, AssemblyReference>();
        private readonly ILogger _logger;

        public Nephila(ILogger logger, string path = "./")
        {
            try
            {
                _logger = logger ?? new NullLogger();
                ProcessAssemblies(path);
            }
            catch (Exception e)
            {
                _logger.Log(e.Message);
            }
        }

        public IEnumerable<List<AssemblyReference>> GetReferenceChains(string assemblyName)
        {
            AssemblyReference ar = _assemblyIndex.Values.Where(x => x.String.IndexOf(assemblyName, StringComparison.CurrentCultureIgnoreCase) >= 0).FirstOrDefault();

            var refChain = new List<AssemblyReference>();
            ConcurrentBag<List<AssemblyReference>> refChains = new ConcurrentBag<List<AssemblyReference>>() { refChain };

            if (ar != null)
            {
                ProcessReferenceChains(ar, refChain, refChains);
            }
            return refChains;
        }

        public HashSet<Tuple<AssemblyReference, AssemblyReference>> GetReferencePairs(string assemblyName, int depth = -1)
        {
            AssemblyReference ar = _assemblyIndex.Values.Where(x => x.String.IndexOf(assemblyName, StringComparison.CurrentCultureIgnoreCase) >= 0).FirstOrDefault();

            var referencePairs = new HashSet<Tuple<AssemblyReference, AssemblyReference>>();

            if (ar != null)
            {
                ProcessReferencePairs(ar, referencePairs, depth);
            }

            return referencePairs;
        }

        private void ProcessReferencePairs(AssemblyReference assemblyReference, HashSet<Tuple<AssemblyReference, AssemblyReference>> referencePairs, int depth)
        {
            if (depth == 0 || assemblyReference.ReferredBy.Count <= 0)
            {
                return;
            }

            var nextDepth = depth > 0 ? depth - 1 : -1;

            foreach (var refferedAssembly in assemblyReference.ReferredBy.Values)
            {
                referencePairs.Add(Tuple.Create(assemblyReference, refferedAssembly));
                ProcessReferencePairs(refferedAssembly, referencePairs, nextDepth);
            }
        }

        public IEnumerable<string> GetAssemblyNames(string name)
        {
            return _assemblyIndex.Values.Where(a => a.String.IndexOf(name, StringComparison.CurrentCultureIgnoreCase) >= 0).Select(a => a.ToString());
        }

        private void ProcessReferenceChains(AssemblyReference assemblyReference, 
            List<AssemblyReference> currentRefChain, ConcurrentBag<List<AssemblyReference>> refChains)
        {
            currentRefChain.Add(assemblyReference);

            if (assemblyReference.ReferredBy.Count <= 0)
            {
                return;
            }

            //fork current ref chain for processing
            for (int i = 0; i < assemblyReference.ReferredBy.Count; ++i)
            {
                List<AssemblyReference> forkRefChian = null;

                if (i < assemblyReference.ReferredBy.Count)
                {
                    forkRefChian = new List<AssemblyReference>(currentRefChain);
                    refChains.Add(forkRefChian);
                }
                else
                {
                    forkRefChian = currentRefChain;
                }

                ProcessReferenceChains(assemblyReference.ReferredBy.ElementAt(i).Value, forkRefChian, refChains);
            }
        }

        private void ProcessAssemblies(string path = "./")
        {
            var files = Directory.GetFiles(path, "*.dll");
            var workingDir = Path.GetFullPath(path);

            foreach(var file in files)
            {
                try
                {
                    var assembly = Assembly.LoadFile(Path.GetFullPath(file));

                    _assemblyIndex.TryAdd(assembly.FullName, new AssemblyReference
                    {
                        FileName = Path.GetFileName(file),
                        Assembly = assembly,
                        Version = assembly.GetName().Version,
                    });
                }
                catch (Exception e)
                {
                    _logger.Log($"Unable to process {file}, Exception: {e.Message}");
                }
            }

            Parallel.ForEach(_assemblyIndex, (assemblyReference) => ProcessAssemblyReferences(assemblyReference.Value));
        }

        private void ProcessAssemblyReferences(AssemblyReference assemblyReference)
        {
            if (!assemblyReference.FileLoaded)
            {
                return;
            }

            var assembly = assemblyReference.Assembly;

            foreach (var refferedAssembly in assembly.GetReferencedAssemblies())
            {
                if (_assemblyIndex.ContainsKey(refferedAssembly.FullName))
                {
                    var referredByEntry = _assemblyIndex[refferedAssembly.FullName];
                    referredByEntry.ReferredBy.TryAdd(assembly.FullName, _assemblyIndex[assembly.FullName]);
                }
                else
                {
                    AssemblyReference unloadedAssemblyReference = new AssemblyReference
                    {
                        FileName = refferedAssembly.Name,
                        Assembly = null,
                        Version = refferedAssembly.Version,
                    };
                    unloadedAssemblyReference.ReferredBy.TryAdd(assembly.FullName, _assemblyIndex[assembly.FullName]);

                    _assemblyIndex.TryAdd(refferedAssembly.FullName, unloadedAssemblyReference);
                }
            }
        }
    }
}
