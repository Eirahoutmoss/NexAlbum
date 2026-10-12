using System.Reflection;
using FluentAssertions;
using Xunit;

namespace NexAlbum.Architecture.Tests;

/// <summary>
/// Derlenmiş katmanların gerçekte kullandığı bağımlılıkları denetler.
/// UI (net10.0-windows) bu test projesinden yüklenemediği için kapsam dışıdır; onu ProjectReferenceTests denetler.
/// </summary>
public class AssemblyReferenceTests
{
    public static TheoryData<string> Libraries()
    {
        var data = new TheoryData<string>();
        foreach (var name in LayerRules.Allowed.Keys.Where(k => k != "NexAlbum.UI"))
        {
            data.Add(name);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Libraries))]
    public void AssemblyReferencesFollowLayerRules(string library)
    {
        var referenced = Assembly.Load(library)
            .GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(n => n.StartsWith("NexAlbum.", StringComparison.Ordinal))
            .ToArray();

        LayerRules.AssertOnlyAllowed(library, referenced);
    }

    [Fact]
    public void CoreDoesNotReferenceTechnologyAssemblies()
    {
        var referenced = Assembly.Load("NexAlbum.Core")
            .GetReferencedAssemblies()
            .Select(a => a.Name!)
            .ToArray();

        referenced.Should().NotIntersectWith(LayerRules.ForbiddenInCore,
            "Core saf domain'dir; UI, render, PDF, veritabanı ve log teknolojilerinden bağımsız kalmalı");
    }
}
