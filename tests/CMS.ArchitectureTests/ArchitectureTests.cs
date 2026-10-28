using System.Reflection;
using System.Xml.Linq;
using NetArchTest.Rules;

namespace CMS.ArchitectureTests;

// Enforce boundaries before feature implementations can introduce coupling.
public sealed class ArchitectureTests
{
    private static readonly string _root = FindRoot();

    // Project references are checked even when an assembly has no feature types yet.
    [Theory]
    [InlineData("CMS.SharedKernel", "")]
    [InlineData("CMS.Domain", "CMS.SharedKernel")]
    [InlineData("CMS.Application", "CMS.Domain")]
    [InlineData("CMS.Infrastructure", "CMS.Application")]
    [InlineData("CMS.Api", "CMS.Application,CMS.Infrastructure")]
    [InlineData("CMS.Migrator", "CMS.Infrastructure")]
    public void Project_references_follow_the_dependency_graph(string project, string expected)
    {
        var document = XDocument.Load(ProjectPath(project));
        var references = document.Descendants("ProjectReference")
            .Select(element => Path.GetFileNameWithoutExtension(
                element.Attribute("Include")!.Value.Replace('\\', '/')))
            .Order(StringComparer.Ordinal).ToArray();

        Assert.Equal(expected.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Order(StringComparer.Ordinal), references);
    }

    // The innermost layers must remain usable without framework packages.
    [Theory]
    [InlineData("CMS.SharedKernel")]
    [InlineData("CMS.Domain")]
    public void Inner_projects_have_no_package_or_framework_references(string project)
    {
        var document = XDocument.Load(ProjectPath(project));
        Assert.Empty(document.Descendants("PackageReference"));
        Assert.Empty(document.Descendants("FrameworkReference"));
    }

    // Inspect emitted type dependencies as well as the declared project graph.
    [Theory]
    [InlineData("CMS.SharedKernel", "CMS.Domain,CMS.Application,CMS.Infrastructure,CMS.Api,Microsoft.EntityFrameworkCore,MediatR")]
    [InlineData("CMS.Domain", "CMS.Application,CMS.Infrastructure,CMS.Api,Microsoft.EntityFrameworkCore,MediatR")]
    [InlineData("CMS.Application", "CMS.Infrastructure,CMS.Api,Microsoft.EntityFrameworkCore,Dapper,StackExchange.Redis,Microsoft.AspNetCore")]
    [InlineData("CMS.Infrastructure", "CMS.Api")]
    [InlineData("CMS.Api", "Microsoft.EntityFrameworkCore,Dapper,StackExchange.Redis")]
    public void Assemblies_do_not_depend_on_forbidden_layers(string assemblyName, string forbidden)
    {
        var result = Types.InAssembly(Assembly.Load(assemblyName)).ShouldNot()
            .HaveDependencyOnAny(forbidden.Split(',')).GetResult();

        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    // Keep content independent of commerce and form extensions as modules grow.
    [Theory]
    [InlineData("CMS.Domain")]
    [InlineData("CMS.Application")]
    public void Content_does_not_depend_on_forms_or_store(string assemblyName)
    {
        var result = Types.InAssembly(Assembly.Load(assemblyName))
            .That().ResideInNamespaceStartingWith(assemblyName + ".Content")
            .ShouldNot().HaveDependencyOnAny(
                "CMS.Domain.Forms", "CMS.Domain.Store", "CMS.Application.Forms", "CMS.Application.Store")
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    // Concrete implementation types should not expose accidental inheritance points.
    [Theory]
    [InlineData("CMS.Application")]
    [InlineData("CMS.Infrastructure")]
    public void Final_implementations_are_sealed(string assemblyName)
    {
        string[] suffixes = ["Handler", "Validator", "Configuration", "Repository"];
        var types = Assembly.Load(assemblyName).GetTypes().Where(type =>
            type.IsClass && !type.IsAbstract && suffixes.Any(suffix =>
                type.Name.EndsWith(suffix, StringComparison.Ordinal)));

        Assert.All(types, type => Assert.True(type.IsSealed, type.FullName));
    }

    // Central version ownership prevents independent project upgrades.
    [Fact]
    public void Package_versions_are_central_and_obsolete_packages_are_absent()
    {
        foreach (var file in Directory.EnumerateFiles(_root, "*.csproj", SearchOption.AllDirectories))
        {
            foreach (var reference in XDocument.Load(file).Descendants("PackageReference"))
            {
                Assert.Null(reference.Attribute("Version"));
                Assert.Null(reference.Element("Version"));
                Assert.Null(reference.Attribute("VersionOverride"));
                Assert.DoesNotContain(reference.Attribute("Include")!.Value,
                    new[] { "Faithlife.Utility.Dapper", "Swashbuckle.AspNetCore" });
            }
        }
    }

    private static string ProjectPath(string project) =>
        Path.Combine(_root, "src", project, project + ".csproj");

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "CMS.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("The CMS solution root was not found.");
    }
}
