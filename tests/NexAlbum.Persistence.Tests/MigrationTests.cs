using System.Text.Json.Nodes;
using FluentAssertions;
using NexAlbum.Core.Projects;
using NexAlbum.Persistence.Format;
using NexAlbum.Persistence.Migrations;
using NexAlbum.Persistence.Packaging;
using Xunit;

namespace NexAlbum.Persistence.Tests;

public class MigrationTests
{
    private sealed class Step(int from, Func<JsonNode, JsonNode>? body = null) : IMigration
    {
        public int FromVersion => from;

        public int ToVersion => from + 1;

        public JsonNode Migrate(JsonNode project)
        {
            var result = body is null ? project : body(project);
            result["schemaVersion"] = ToVersion;
            result["log"] = (result["log"]?.GetValue<string>() ?? string.Empty) + $"{from}>";
            return result;
        }
    }

    private sealed class Broken(int from, int to) : IMigration
    {
        public int FromVersion => from;

        public int ToVersion => to;

        public JsonNode Migrate(JsonNode project) => project;
    }

    [Fact]
    public void ChainRunsStepsInOrder()
    {
        var chain = new MigrationChain([new Step(2), new Step(1), new Step(3)]);
        var result = chain.Run(new JsonObject { ["schemaVersion"] = 1 }, 1, 4);
        result["schemaVersion"]!.GetValue<int>().Should().Be(4);
        result["log"]!.GetValue<string>().Should().Be("1>2>3>");
    }

    [Fact]
    public void SameVersionIsANoOp()
    {
        var node = new JsonObject { ["schemaVersion"] = 1 };
        MigrationChain.Default.Run(node, 1, 1).Should().BeSameAs(node);
    }

    [Fact]
    public void GapsAreReportedAsMigrationFailure()
    {
        var chain = new MigrationChain([new Step(1)]);
        FluentActions.Invoking(() => chain.Run(new JsonObject { ["schemaVersion"] = 1 }, 1, 3))
            .Should().Throw<ProjectFormatException>().Which.Error.Should().Be(ProjectLoadError.MigrationFailed);
    }

    [Fact]
    public void InvalidChainsAreRejected()
    {
        FluentActions.Invoking(() => new MigrationChain([new Broken(1, 3)])).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => new MigrationChain([new Step(1), new Step(1)])).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void StepThatForgetsVersionOrThrowsFails()
    {
        var forgetful = new MigrationChain([new Broken(1, 2)]);
        FluentActions.Invoking(() => forgetful.Run(new JsonObject { ["schemaVersion"] = 1 }, 1, 2))
            .Should().Throw<ProjectFormatException>().WithMessage("*schemaVersion*");

        var throwing = new MigrationChain([new Step(1, _ => throw new InvalidOperationException("boom"))]);
        FluentActions.Invoking(() => throwing.Run(new JsonObject { ["schemaVersion"] = 1 }, 1, 2))
            .Should().Throw<ProjectFormatException>().Which.InnerException.Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    public void OldProjectIsMigratedOnLoadAndBackedUp()
    {
        using var dir = new TempDir();
        var path = dir.File("eski.albumproj");

        // v0 → v1: eski sürüm "title" alanını "name" olarak yeniden adlandırır.
        var old = PackageBuilder.EditJson(PackageBuilder.Valid(), ProjectFormat.ProjectEntry, n =>
        {
            n["schemaVersion"] = 0;
            n["title"] = n["name"]!.GetValue<string>();
            n.AsObject().Remove("name");
        });
        File.WriteAllBytes(path, old);

        var renameTitle = new Step(0, n =>
        {
            n["name"] = n["title"]!.GetValue<string>();
            n.AsObject().Remove("title");
            return n;
        });

        var loaded = ProjectPackageReader.Load(path, new LoadOptions { Migrations = new MigrationChain([renameTitle, .. MigrationChain.BuiltIn]) });

        loaded.Migrated.Should().BeTrue();
        loaded.OriginalSchemaVersion.Should().Be(0);
        loaded.Project.Name.Should().Be("Ahmet & Zeynep Düğün Albümü");
        loaded.MigrationBackup.Should().Be(path + ".bak-v0");
        File.ReadAllBytes(loaded.MigrationBackup!).Should().Equal(old, "yedek orijinalin birebir kopyası olmalı");
        File.ReadAllBytes(path).Should().Equal(old, "açmak dosyayı değiştirmemeli");
    }

    [Fact]
    public void MigrationWithoutBackupWhenDisabled()
    {
        using var dir = new TempDir();
        var path = dir.File("eski.albumproj");
        File.WriteAllBytes(path, PackageBuilder.EditJson(PackageBuilder.Valid(), ProjectFormat.ProjectEntry, n => n["schemaVersion"] = 0));

        var loaded = ProjectPackageReader.Load(path, new LoadOptions { Migrations = new MigrationChain([new Step(0), .. MigrationChain.BuiltIn]), BackupBeforeMigration = false });
        loaded.Migrated.Should().BeTrue();
        loaded.MigrationBackup.Should().BeNull();
        File.Exists(path + ".bak-v0").Should().BeFalse();
    }

    [Fact]
    public void OldProjectWithoutMigrationFailsSafely()
    {
        var old = PackageBuilder.EditJson(PackageBuilder.Valid(), ProjectFormat.ProjectEntry, n => n["schemaVersion"] = 0);
        PackageBuilder.LoadFails(old).Error.Should().Be(ProjectLoadError.MigrationFailed);
    }

    [Fact]
    public void V1ProjectIsUpgradedToV2WithoutChangingContent()
    {
        using var dir = new TempDir();
        var path = dir.File("v1.albumproj");
        var v1 = PackageBuilder.EditJson(PackageBuilder.Valid(), ProjectFormat.ProjectEntry, n => n["schemaVersion"] = 1);
        File.WriteAllBytes(path, v1);

        var loaded = ProjectPackageReader.Load(path, new LoadOptions { BackupBeforeMigration = false });

        loaded.Migrated.Should().BeTrue();
        loaded.OriginalSchemaVersion.Should().Be(1);
        loaded.Project.Pages.Should().HaveCount(Samples.FullProject().Pages.Count);
        loaded.Project.Pages.SelectMany(p => p.Slots).Should().OnlyContain(s => s.Polygon == null, "v1'de çokgen yuva yoktur");
    }

    [Fact]
    public void V2ProjectIsUpgradedToV3WithTextsWithoutEffects()
    {
        using var dir = new TempDir();
        var path = dir.File("v2.albumproj");
        var v2 = PackageBuilder.EditJson(PackageBuilder.Valid(), ProjectFormat.ProjectEntry, n =>
        {
            n["schemaVersion"] = 2;
            foreach (var element in n["pages"]!.AsArray().SelectMany(p => p!["elements"]!.AsArray()))
            {
                element!.AsObject().Remove("effect");
            }
        });
        File.WriteAllBytes(path, v2);

        var loaded = ProjectPackageReader.Load(path, new LoadOptions { BackupBeforeMigration = false });

        loaded.Migrated.Should().BeTrue();
        loaded.OriginalSchemaVersion.Should().Be(2);
        var texts = loaded.Project.Pages.SelectMany(p => p.Elements.OfType<TextElement>()).ToList();
        texts.Should().NotBeEmpty();
        texts.Should().OnlyContain(t => t.Effect.IsNone, "v2'de metin etkisi yoktur");
    }

    [Fact]
    public void V3ProjectIsUpgradedToV4AsASingleFlow()
    {
        using var dir = new TempDir();
        var path = dir.File("v3.albumproj");
        var v3 = PackageBuilder.EditJson(PackageBuilder.Valid(), ProjectFormat.ProjectEntry, n =>
        {
            n["schemaVersion"] = 3;
            n.AsObject().Remove("story");
        });
        File.WriteAllBytes(path, v3);

        var loaded = ProjectPackageReader.Load(path, new LoadOptions { BackupBeforeMigration = false });

        loaded.Migrated.Should().BeTrue();
        loaded.OriginalSchemaVersion.Should().Be(3);
        loaded.Project.Story.HasChapters.Should().BeFalse("v3'te hikâye akışı yoktur");
        loaded.Project.Story.Enabled.Should().BeTrue();
    }

    [Fact]
    public void V4ProjectIsUpgradedToV5WithDefaultVideoSettings()
    {
        using var dir = new TempDir();
        var path = dir.File("v4.albumproj");
        var v4 = PackageBuilder.EditJson(PackageBuilder.Valid(), ProjectFormat.ProjectEntry, n =>
        {
            n["schemaVersion"] = 4;
            n.AsObject().Remove("video");
        });
        File.WriteAllBytes(path, v4);

        var loaded = ProjectPackageReader.Load(path, new LoadOptions { BackupBeforeMigration = false });

        loaded.OriginalSchemaVersion.Should().Be(4);
        loaded.Project.Video.Should().Be(Core.Video.VideoSettings.Default);
    }

    [Fact]
    public void BuiltInChainReachesTheCurrentVersion()
    {
        var node = new JsonObject { ["schemaVersion"] = 1 };
        MigrationChain.Default.Run(node, 1, ProjectFormat.CurrentSchemaVersion)["schemaVersion"]!.GetValue<int>()
            .Should().Be(ProjectFormat.CurrentSchemaVersion);
        MigrationChain.BuiltIn.Should().HaveCount(ProjectFormat.CurrentSchemaVersion - 1, "her sürüm artışı bir göç adımı ister (Karar 09 §14)");
    }
}
