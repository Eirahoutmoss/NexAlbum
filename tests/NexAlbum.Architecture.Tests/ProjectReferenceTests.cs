using System.Xml.Linq;
using FluentAssertions;
using Xunit;

namespace NexAlbum.Architecture.Tests;

/// <summary>
/// .csproj dosyalarındaki ProjectReference'ları denetler; ihlal derleme anında yakalanır.
/// </summary>
public class ProjectReferenceTests
{
    public static TheoryData<string> Projects()
    {
        var data = new TheoryData<string>();
        foreach (var name in LayerRules.Allowed.Keys)
        {
            data.Add(name);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Projects))]
    public void ProjectReferencesFollowLayerRules(string project)
    {
        var csproj = Path.Combine(RepoRoot.Find(), "src", project, project + ".csproj");
        File.Exists(csproj).Should().BeTrue($"{project} projesi src/ altında olmalı");

        var references = XDocument.Load(csproj)
            .Descendants("ProjectReference")
            .Select(e => Path.GetFileNameWithoutExtension(e.Attribute("Include")!.Value.Replace('\\', '/')))
            .ToArray();

        LayerRules.AssertOnlyAllowed(project, references);
    }

    [Fact]
    public void EverySourceProjectHasALayerRule()
    {
        var projects = Directory.GetDirectories(Path.Combine(RepoRoot.Find(), "src"))
            .Select(d => Path.GetFileName(d))
            .ToArray();

        projects.Should().BeEquivalentTo(LayerRules.Allowed.Keys,
            "yeni bir katman eklenirse önce LayerRules ve Karar 08 güncellenmeli");
    }
}
