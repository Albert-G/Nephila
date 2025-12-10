namespace Nephila.Console.UnitTest
{
    [TestClass]
    public class ParseInputTests
    {
        [TestMethod]
        public void ParseInput_WithAssemblyNameAndDepth_ShouldParseCorrectly()
        {
            // Arrange
            var input = "MyAssembly 5";

            // Act
            Program.ParseInput(input, out var assemblyName, out var recursiveDepth);

            // Assert
            Assert.AreEqual("MyAssembly", assemblyName);
            Assert.AreEqual(5, recursiveDepth);
        }

        [TestMethod]
        public void ParseInput_WithOnlyAssemblyName_ShouldUseDefaultDepth()
        {
            // Arrange
            var input = "MyAssembly";

            // Act
            Program.ParseInput(input, out var assemblyName, out var recursiveDepth);

            // Assert
            Assert.AreEqual("MyAssembly", assemblyName);
            Assert.AreEqual(3, recursiveDepth); // Assuming default is 3
        }

        [TestMethod]
        public void ParseInput_WithExtraSpaces_ShouldParseCorrectly()
        {
            // Arrange
            var input = "  MyAssembly   2  ";

            // Act
            Program.ParseInput(input.Trim(), out var assemblyName, out var recursiveDepth);

            // Assert
            Assert.AreEqual("MyAssembly", assemblyName);
            Assert.AreEqual(2, recursiveDepth);
        }

        [TestMethod]
        public void ParseInput_EmptyInput_ShouldReturnEmptyAndDefaultDepth()
        {
            // Arrange
            var input = "";

            // Act
            Program.ParseInput(input, out var assemblyName, out var recursiveDepth);

            // Assert
            Assert.AreEqual(string.Empty, assemblyName);
            Assert.AreEqual(3, recursiveDepth); // Assuming default is 3
        }

        [TestMethod]
        public void ParseInput_WithInvalidDepth_ShouldUseDefaultDepth()
        {
            // Arrange
            var input = "MyAssembly abc";

            // Act
            Program.ParseInput(input, out var assemblyName, out var recursiveDepth);

            // Assert
            Assert.AreEqual("MyAssembly abc", assemblyName);
            Assert.AreEqual(3, recursiveDepth); // Assuming default is 3
        }

        [TestMethod]
        public void ParseInput_WithComplexAssemblyNameAndDepth_ShouldParseCorrectly()
        {
            // Arrange
            var input = "My.Assembly (1.0.0.0) (some other info) 5";

            // Act
            Program.ParseInput(input, out var assemblyName, out var recursiveDepth);

            // Assert
            Assert.AreEqual("My.Assembly (1.0.0.0) (some other info)", assemblyName);
            Assert.AreEqual(5, recursiveDepth);
        }

        [TestMethod]
        public void ParseInput_WithComplexAssemblyNameAndNoDepth_ShouldUseDefaultDepth()
        {
            // Arrange
            var input = "My.Assembly (1.0.0.0) (some other info)";

            // Act
            Program.ParseInput(input, out var assemblyName, out var recursiveDepth);

            // Assert
            Assert.AreEqual("My.Assembly (1.0.0.0) (some other info)", assemblyName);
            Assert.AreEqual(3, recursiveDepth); // Assuming default is 3
        }
    }
}
