using ApiForge.Desktop.ViewModels;
using ApiForge.Domain.GeneratedApiSolution;

namespace ApiForge.Desktop.Tests.ViewModels
{
    /// <summary>
    /// Tests for the <see cref="FileNodeViewModel"/> class.
    /// </summary>
    public class FileNodeViewModelTests
    {
        private static GeneratedFile File(string path, string content = "x") =>
            new() { RelativePath = path, Content = content };

        /// <summary>
        /// Tests that BuildTree creates one folder node per path segment and a file leaf.
        /// </summary>
        [Fact]
        public void BuildTree_NestedPath_CreatesFolderHierarchy()
        {
            var tree = FileNodeViewModel.BuildTree([File("Shop.Domain/Models/Pet.cs", "class Pet")]);

            var domain = Assert.Single(tree);
            Assert.Equal("Shop.Domain", domain.Name);
            Assert.False(domain.IsFile);

            var models = Assert.Single(domain.Children);
            Assert.Equal("Models", models.Name);

            var pet = Assert.Single(models.Children);
            Assert.Equal("Pet.cs", pet.Name);
            Assert.True(pet.IsFile);
            Assert.Equal("class Pet", pet.Content);
        }

        /// <summary>
        /// Tests that BuildTree reuses the same folder node for files that share a folder.
        /// </summary>
        [Fact]
        public void BuildTree_FilesInSameFolder_ShareFolderNode()
        {
            var tree = FileNodeViewModel.BuildTree([File("Shop.Domain/A.cs"), File("Shop.Domain/B.cs")]);

            var folder = Assert.Single(tree);
            Assert.Equal(2, folder.Children.Count);
        }

        /// <summary>
        /// Tests that BuildTree sorts folders before files and ignores casing within each group.
        /// </summary>
        [Fact]
        public void BuildTree_SortsFoldersFirst_ThenByNameIgnoringCase()
        {
            var tree = FileNodeViewModel.BuildTree(
            [
                File("b.cs"),
                File("A.cs"),
                File("Zed/x.cs"),
                File("alpha/y.cs")
            ]);

            Assert.Equal(["alpha", "Zed", "A.cs", "b.cs"], tree.Select(n => n.Name));
        }

        /// <summary>
        /// Tests that BuildTree skips entries whose path has no usable segments.
        /// </summary>
        [Theory]
        [InlineData("")]
        [InlineData("///")]
        public void BuildTree_EmptyPaths_AreIgnored(string path)
        {
            var tree = FileNodeViewModel.BuildTree([File(path)]);

            Assert.Empty(tree);
        }

        /// <summary>
        /// Tests that a file with empty content is still treated as a file, not as a folder.
        /// </summary>
        [Fact]
        public void BuildTree_EmptyContentFile_IsStillAFile()
        {
            var tree = FileNodeViewModel.BuildTree([File("empty.txt", string.Empty)]);

            var node = Assert.Single(tree);
            Assert.True(node.IsFile);
            Assert.Equal(string.Empty, node.Content);
        }

        /// <summary>
        /// Tests that the ToString override of <see cref="ArchitectureOption"/> returns its label.
        /// </summary>
        [Fact]
        public void ArchitectureOption_ToString_ReturnsLabel() =>
            Assert.Equal("Clean Architecture", new ArchitectureOption("Clean Architecture", Domain.Enums.ArchitectureStyle.Clean).ToString());
    }
}
