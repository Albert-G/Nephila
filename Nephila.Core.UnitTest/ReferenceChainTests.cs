using System.Collections.Concurrent;
using System.Reflection;

namespace Nephila.Core.UnitTest
{
    [TestClass]
    public class ReferenceChainTests
    {
        /// <summary>
        /// Creates a Nephila instance with an empty assembly index (no disk I/O)
        /// and populates it via reflection for isolated testing.
        /// </summary>
        private static global::Nephila.Nephila CreateNephilaWithAssemblies(params AssemblyReference[] assemblies)
        {
            // Create instance with a non-existent path so ProcessAssemblies finds no files
            var nephila = new global::Nephila.Nephila(new NullLogger(), Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

            var indexField = typeof(global::Nephila.Nephila).GetField("_assemblyIndex", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var index = (ConcurrentDictionary<string, AssemblyReference>)indexField.GetValue(nephila)!;

            foreach (var ar in assemblies)
            {
                index.TryAdd(ar.String, ar);
            }

            return nephila;
        }

        private static AssemblyReference CreateAssemblyRef(string fileName, string version = "1.0.0.0")
        {
            return new AssemblyReference
            {
                FileName = fileName,
                Assembly = null,
                Version = Version.Parse(version),
            };
        }

        #region GetReferenceChains Tests

        [TestMethod]
        public void GetReferenceChains_AssemblyNotFound_ReturnsEmptyChain()
        {
            var nephila = CreateNephilaWithAssemblies();

            var chains = nephila.GetReferenceChains("NonExistent").ToList();

            // The initial empty chain is always present
            Assert.AreEqual(1, chains.Count);
            Assert.AreEqual(0, chains[0].Count);
        }

        [TestMethod]
        public void GetReferenceChains_AssemblyWithNoReferrers_ReturnsSingleChainWithSelf()
        {
            var assemblyA = CreateAssemblyRef("AssemblyA");
            var nephila = CreateNephilaWithAssemblies(assemblyA);

            var chains = nephila.GetReferenceChains("AssemblyA").ToList();

            Assert.AreEqual(1, chains.Count);
            Assert.AreEqual(1, chains[0].Count);
            Assert.AreEqual("AssemblyA", chains[0][0].FileName);
        }

        [TestMethod]
        public void GetReferenceChains_SingleReferrer_ReturnsChainOfTwo()
        {
            // AssemblyA is referred by AssemblyB
            var assemblyA = CreateAssemblyRef("AssemblyA");
            var assemblyB = CreateAssemblyRef("AssemblyB");
            assemblyA.ReferredBy.TryAdd(assemblyB.String, assemblyB);

            var nephila = CreateNephilaWithAssemblies(assemblyA, assemblyB);

            var chains = nephila.GetReferenceChains("AssemblyA").ToList();

            // One of the chains should be [A, B]
            var validChain = chains.FirstOrDefault(c => c.Count == 2 && c[0].FileName == "AssemblyA" && c[1].FileName == "AssemblyB");
            Assert.IsNotNull(validChain, "Expected a chain [AssemblyA -> AssemblyB]");
        }

        [TestMethod]
        public void GetReferenceChains_MultipleReferrers_ReturnsForks()
        {
            // AssemblyA is referred by both AssemblyB and AssemblyC
            var assemblyA = CreateAssemblyRef("AssemblyA");
            var assemblyB = CreateAssemblyRef("AssemblyB");
            var assemblyC = CreateAssemblyRef("AssemblyC");
            assemblyA.ReferredBy.TryAdd(assemblyB.String, assemblyB);
            assemblyA.ReferredBy.TryAdd(assemblyC.String, assemblyC);

            var nephila = CreateNephilaWithAssemblies(assemblyA, assemblyB, assemblyC);

            var chains = nephila.GetReferenceChains("AssemblyA").ToList();

            // Should have chains ending in B and C
            var chainEndingInB = chains.Any(c => c.Count == 2 && c.Last().FileName == "AssemblyB");
            var chainEndingInC = chains.Any(c => c.Count == 2 && c.Last().FileName == "AssemblyC");
            Assert.IsTrue(chainEndingInB, "Expected a chain ending in AssemblyB");
            Assert.IsTrue(chainEndingInC, "Expected a chain ending in AssemblyC");
        }

        [TestMethod]
        public void GetReferenceChains_DeepChain_ReturnsFullDepthChain()
        {
            // A -> B -> C (A referred by B, B referred by C)
            var assemblyA = CreateAssemblyRef("AssemblyA");
            var assemblyB = CreateAssemblyRef("AssemblyB");
            var assemblyC = CreateAssemblyRef("AssemblyC");
            assemblyA.ReferredBy.TryAdd(assemblyB.String, assemblyB);
            assemblyB.ReferredBy.TryAdd(assemblyC.String, assemblyC);

            var nephila = CreateNephilaWithAssemblies(assemblyA, assemblyB, assemblyC);

            var chains = nephila.GetReferenceChains("AssemblyA").ToList();

            var deepChain = chains.FirstOrDefault(c => c.Count == 3);
            Assert.IsNotNull(deepChain, "Expected a chain of depth 3");
            Assert.AreEqual("AssemblyA", deepChain[0].FileName);
            Assert.AreEqual("AssemblyB", deepChain[1].FileName);
            Assert.AreEqual("AssemblyC", deepChain[2].FileName);
        }

        [TestMethod]
        public void GetReferenceChains_DiamondShape_ReturnsBothPaths()
        {
            // A referred by B and C, both B and C referred by D
            var assemblyA = CreateAssemblyRef("AssemblyA");
            var assemblyB = CreateAssemblyRef("AssemblyB");
            var assemblyC = CreateAssemblyRef("AssemblyC");
            var assemblyD = CreateAssemblyRef("AssemblyD");
            assemblyA.ReferredBy.TryAdd(assemblyB.String, assemblyB);
            assemblyA.ReferredBy.TryAdd(assemblyC.String, assemblyC);
            assemblyB.ReferredBy.TryAdd(assemblyD.String, assemblyD);
            assemblyC.ReferredBy.TryAdd(assemblyD.String, assemblyD);

            var nephila = CreateNephilaWithAssemblies(assemblyA, assemblyB, assemblyC, assemblyD);

            var chains = nephila.GetReferenceChains("AssemblyA").ToList();

            // Should have chains A->B->D and A->C->D
            var chainViaB = chains.Any(c => c.Count == 3 && c[1].FileName == "AssemblyB" && c[2].FileName == "AssemblyD");
            var chainViaC = chains.Any(c => c.Count == 3 && c[1].FileName == "AssemblyC" && c[2].FileName == "AssemblyD");
            Assert.IsTrue(chainViaB, "Expected chain A->B->D");
            Assert.IsTrue(chainViaC, "Expected chain A->C->D");
        }

        [TestMethod]
        public void GetReferenceChains_CaseInsensitiveLookup_FindsAssembly()
        {
            var assemblyA = CreateAssemblyRef("MyLibrary");
            var nephila = CreateNephilaWithAssemblies(assemblyA);

            var chains = nephila.GetReferenceChains("mylibrary").ToList();

            var validChain = chains.FirstOrDefault(c => c.Count == 1 && c[0].FileName == "MyLibrary");
            Assert.IsNotNull(validChain, "Case-insensitive lookup should find the assembly");
        }

        [TestMethod]
        public void GetReferenceChains_PartialNameMatch_FindsAssembly()
        {
            var assemblyA = CreateAssemblyRef("My.Long.Assembly.Name");
            var nephila = CreateNephilaWithAssemblies(assemblyA);

            var chains = nephila.GetReferenceChains("Long.Assembly").ToList();

            var validChain = chains.FirstOrDefault(c => c.Count == 1 && c[0].FileName == "My.Long.Assembly.Name");
            Assert.IsNotNull(validChain, "Partial name match should find the assembly");
        }

        #endregion

        #region GetReferencePairs Tests

        [TestMethod]
        public void GetReferencePairs_AssemblyNotFound_ReturnsEmptySet()
        {
            var nephila = CreateNephilaWithAssemblies();

            var pairs = nephila.GetReferencePairs("NonExistent");

            Assert.AreEqual(0, pairs.Count);
        }

        [TestMethod]
        public void GetReferencePairs_NoReferrers_ReturnsEmptySet()
        {
            var assemblyA = CreateAssemblyRef("AssemblyA");
            var nephila = CreateNephilaWithAssemblies(assemblyA);

            var pairs = nephila.GetReferencePairs("AssemblyA");

            Assert.AreEqual(0, pairs.Count);
        }

        [TestMethod]
        public void GetReferencePairs_SingleReferrer_ReturnsSinglePair()
        {
            var assemblyA = CreateAssemblyRef("AssemblyA");
            var assemblyB = CreateAssemblyRef("AssemblyB");
            assemblyA.ReferredBy.TryAdd(assemblyB.String, assemblyB);

            var nephila = CreateNephilaWithAssemblies(assemblyA, assemblyB);

            var pairs = nephila.GetReferencePairs("AssemblyA");

            Assert.AreEqual(1, pairs.Count);
            var pair = pairs.First();
            Assert.AreEqual("AssemblyA", pair.Item1.FileName);
            Assert.AreEqual("AssemblyB", pair.Item2.FileName);
        }

        [TestMethod]
        public void GetReferencePairs_DeepChain_ReturnsAllPairs()
        {
            // A -> B -> C
            var assemblyA = CreateAssemblyRef("AssemblyA");
            var assemblyB = CreateAssemblyRef("AssemblyB");
            var assemblyC = CreateAssemblyRef("AssemblyC");
            assemblyA.ReferredBy.TryAdd(assemblyB.String, assemblyB);
            assemblyB.ReferredBy.TryAdd(assemblyC.String, assemblyC);

            var nephila = CreateNephilaWithAssemblies(assemblyA, assemblyB, assemblyC);

            var pairs = nephila.GetReferencePairs("AssemblyA");

            Assert.AreEqual(2, pairs.Count);
            Assert.IsTrue(pairs.Any(p => p.Item1.FileName == "AssemblyA" && p.Item2.FileName == "AssemblyB"));
            Assert.IsTrue(pairs.Any(p => p.Item1.FileName == "AssemblyB" && p.Item2.FileName == "AssemblyC"));
        }

        [TestMethod]
        public void GetReferencePairs_WithDepthLimit_RespectsDepth()
        {
            // A -> B -> C -> D, depth=1 should only return (A, B)
            var assemblyA = CreateAssemblyRef("AssemblyA");
            var assemblyB = CreateAssemblyRef("AssemblyB");
            var assemblyC = CreateAssemblyRef("AssemblyC");
            var assemblyD = CreateAssemblyRef("AssemblyD");
            assemblyA.ReferredBy.TryAdd(assemblyB.String, assemblyB);
            assemblyB.ReferredBy.TryAdd(assemblyC.String, assemblyC);
            assemblyC.ReferredBy.TryAdd(assemblyD.String, assemblyD);

            var nephila = CreateNephilaWithAssemblies(assemblyA, assemblyB, assemblyC, assemblyD);

            var pairs = nephila.GetReferencePairs("AssemblyA", depth: 1);

            Assert.AreEqual(1, pairs.Count);
            var pair = pairs.First();
            Assert.AreEqual("AssemblyA", pair.Item1.FileName);
            Assert.AreEqual("AssemblyB", pair.Item2.FileName);
        }

        [TestMethod]
        public void GetReferencePairs_WithDepthTwo_ReturnsTwoLevels()
        {
            // A -> B -> C -> D, depth=2 should return (A,B) and (B,C)
            var assemblyA = CreateAssemblyRef("AssemblyA");
            var assemblyB = CreateAssemblyRef("AssemblyB");
            var assemblyC = CreateAssemblyRef("AssemblyC");
            var assemblyD = CreateAssemblyRef("AssemblyD");
            assemblyA.ReferredBy.TryAdd(assemblyB.String, assemblyB);
            assemblyB.ReferredBy.TryAdd(assemblyC.String, assemblyC);
            assemblyC.ReferredBy.TryAdd(assemblyD.String, assemblyD);

            var nephila = CreateNephilaWithAssemblies(assemblyA, assemblyB, assemblyC, assemblyD);

            var pairs = nephila.GetReferencePairs("AssemblyA", depth: 2);

            Assert.AreEqual(2, pairs.Count);
            Assert.IsTrue(pairs.Any(p => p.Item1.FileName == "AssemblyA" && p.Item2.FileName == "AssemblyB"));
            Assert.IsTrue(pairs.Any(p => p.Item1.FileName == "AssemblyB" && p.Item2.FileName == "AssemblyC"));
        }

        [TestMethod]
        public void GetReferencePairs_DepthZero_ReturnsNoPairs()
        {
            var assemblyA = CreateAssemblyRef("AssemblyA");
            var assemblyB = CreateAssemblyRef("AssemblyB");
            assemblyA.ReferredBy.TryAdd(assemblyB.String, assemblyB);

            var nephila = CreateNephilaWithAssemblies(assemblyA, assemblyB);

            var pairs = nephila.GetReferencePairs("AssemblyA", depth: 0);

            Assert.AreEqual(0, pairs.Count);
        }

        [TestMethod]
        public void GetReferencePairs_UnlimitedDepth_ReturnsAllPairs()
        {
            // A -> B -> C -> D, depth=-1 (unlimited)
            var assemblyA = CreateAssemblyRef("AssemblyA");
            var assemblyB = CreateAssemblyRef("AssemblyB");
            var assemblyC = CreateAssemblyRef("AssemblyC");
            var assemblyD = CreateAssemblyRef("AssemblyD");
            assemblyA.ReferredBy.TryAdd(assemblyB.String, assemblyB);
            assemblyB.ReferredBy.TryAdd(assemblyC.String, assemblyC);
            assemblyC.ReferredBy.TryAdd(assemblyD.String, assemblyD);

            var nephila = CreateNephilaWithAssemblies(assemblyA, assemblyB, assemblyC, assemblyD);

            var pairs = nephila.GetReferencePairs("AssemblyA", depth: -1);

            Assert.AreEqual(3, pairs.Count);
        }

        [TestMethod]
        public void GetReferencePairs_MultipleReferrers_ReturnsAllBranches()
        {
            // A referred by B and C
            var assemblyA = CreateAssemblyRef("AssemblyA");
            var assemblyB = CreateAssemblyRef("AssemblyB");
            var assemblyC = CreateAssemblyRef("AssemblyC");
            assemblyA.ReferredBy.TryAdd(assemblyB.String, assemblyB);
            assemblyA.ReferredBy.TryAdd(assemblyC.String, assemblyC);

            var nephila = CreateNephilaWithAssemblies(assemblyA, assemblyB, assemblyC);

            var pairs = nephila.GetReferencePairs("AssemblyA");

            Assert.AreEqual(2, pairs.Count);
            Assert.IsTrue(pairs.Any(p => p.Item1.FileName == "AssemblyA" && p.Item2.FileName == "AssemblyB"));
            Assert.IsTrue(pairs.Any(p => p.Item1.FileName == "AssemblyA" && p.Item2.FileName == "AssemblyC"));
        }

        #endregion

        #region GetAssemblyNames Tests

        [TestMethod]
        public void GetAssemblyNames_NoMatch_ReturnsEmpty()
        {
            var assemblyA = CreateAssemblyRef("AssemblyA");
            var nephila = CreateNephilaWithAssemblies(assemblyA);

            var names = nephila.GetAssemblyNames("NonExistent").ToList();

            Assert.AreEqual(0, names.Count);
        }

        [TestMethod]
        public void GetAssemblyNames_ExactMatch_ReturnsAssembly()
        {
            var assemblyA = CreateAssemblyRef("AssemblyA");
            var nephila = CreateNephilaWithAssemblies(assemblyA);

            var names = nephila.GetAssemblyNames("AssemblyA").ToList();

            Assert.AreEqual(1, names.Count);
            Assert.IsTrue(names[0].Contains("AssemblyA"));
        }

        [TestMethod]
        public void GetAssemblyNames_PartialMatch_ReturnsMatchingAssemblies()
        {
            var assemblyA = CreateAssemblyRef("My.Assembly.One");
            var assemblyB = CreateAssemblyRef("My.Assembly.Two");
            var assemblyC = CreateAssemblyRef("Other.Lib");
            var nephila = CreateNephilaWithAssemblies(assemblyA, assemblyB, assemblyC);

            var names = nephila.GetAssemblyNames("My.Assembly").ToList();

            Assert.AreEqual(2, names.Count);
        }

        [TestMethod]
        public void GetAssemblyNames_CaseInsensitive_ReturnsMatch()
        {
            var assemblyA = CreateAssemblyRef("MyLibrary");
            var nephila = CreateNephilaWithAssemblies(assemblyA);

            var names = nephila.GetAssemblyNames("mylibrary").ToList();

            Assert.AreEqual(1, names.Count);
        }

        #endregion

        #region AssemblyReference Tests

        [TestMethod]
        public void AssemblyReference_String_IncludesUnloadedSuffix_WhenNoAssembly()
        {
            var ar = CreateAssemblyRef("TestLib", "2.0.0.0");

            Assert.IsTrue(ar.String.Contains("(Unloaded)"));
            Assert.IsTrue(ar.String.Contains("TestLib"));
            Assert.IsTrue(ar.String.Contains("2.0.0.0"));
        }

        [TestMethod]
        public void AssemblyReference_FileLoaded_IsFalse_WhenAssemblyIsNull()
        {
            var ar = CreateAssemblyRef("TestLib");

            Assert.IsFalse(ar.FileLoaded);
        }

        [TestMethod]
        public void AssemblyReference_ToString_ReturnsSameAsString()
        {
            var ar = CreateAssemblyRef("TestLib", "3.0.0.0");

            Assert.AreEqual(ar.String, ar.ToString());
        }

        #endregion
    }
}
