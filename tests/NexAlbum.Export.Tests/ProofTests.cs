using FluentAssertions;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Profiles;
using NexAlbum.Core.Projects;
using NexAlbum.Export.Proof;
using Xunit;

namespace NexAlbum.Export.Tests;

public class ProofTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Id = Guid.Parse("11111111-2222-3333-4444-555555555555");

    private static AlbumProject Project()
    {
        var profile = new AlbumProfile { Id = "p", Name = "30×30", Page = new PageSpec(new SizeMm(300, 300)) };
        return AlbumProject.Create("Yaz <2026>", profile, Now, "0.1.0", Id).WithPages(
        [
            new AlbumPage { Id = "page-001", Notes = "Tasarımcı: renkleri kontrol et" },
            new AlbumPage { Id = "page-002" },
            new AlbumPage { Id = "page-003" },
        ]);
    }

    private static string Feedback(Guid id, string pages, string general = "Harika olmuş") => $$"""
        { "format": "nexalbum-proof-feedback", "version": 1, "projectId": "{{id}}", "projectName": "Yaz", "general": "{{general}}", "pages": [{{pages}}] }
        """;

    [Fact]
    public void DocumentIsSelfContainedAndEscaped()
    {
        var html = ProofDocument.Build("Yaz <2026>", Id, [new ProofPage("page-001", "Sayfa 1", [0xFF, 0xD8, 0xFF], 10, 10)], Now);

        html.Should().StartWith("<!doctype html>");
        html.Should().Contain("Yaz &lt;2026&gt;").And.NotContain("<title>Yaz <2026>");
        html.Should().Contain("data:image/jpeg;base64,/9j/");
        html.Should().Contain(Id.ToString()).And.Contain("nexalbum-proof-feedback");
        html.Should().NotContain("http://").And.NotContain("https://", "çevrimdışı; dış kaynak yok");
        html.Should().Contain("data-id=\"page-001\"");
    }

    [Fact]
    public void FeedbackIsWrittenToPageNotesAndKeepsDesignerNotes()
    {
        var feedback = ProofFeedback.Parse(Feedback(Id, """
            { "id": "page-001", "liked": true, "note": "Bu fotoğraf\nçok güzel" },
            { "id": "page-002", "liked": false, "note": "" },
            { "id": "page-003", "liked": false, "note": "Başka fotoğraf olsun" },
            { "id": "page-999", "liked": true, "note": "silinmiş sayfa" }
            """));

        var result = feedback.ApplyTo(Project(), Now);

        result.Liked.Should().Be(1);
        result.Notes.Should().Be(2);
        result.Unknown.Should().Be(1);
        var pages = result.Project.Pages;
        pages[0].Notes.Should().Be("Tasarımcı: renkleri kontrol et\nProva: ♥ beğenildi · Bu fotoğraf / çok güzel");
        pages[1].Notes.Should().BeNull("geri bildirim yoksa dokunulmaz");
        pages[2].Notes.Should().Be("Prova: Başka fotoğraf olsun");
        result.Project.Description.Should().Be("Prova: Harika olmuş");

        // İkinci prova öncekinin yerine geçer.
        var again = ProofFeedback.Parse(Feedback(Id, """{ "id": "page-001", "liked": false, "note": "Tamam" }""", general: "")).ApplyTo(result.Project, Now);
        again.Project.Pages[0].Notes.Should().Be("Tasarımcı: renkleri kontrol et\nProva: Tamam");
    }

    [Fact]
    public void ForeignOrBrokenFilesAreRejected()
    {
        var other = ProofFeedback.Parse(Feedback(Guid.NewGuid(), """{ "id": "page-001", "liked": true }"""));
        FluentActions.Invoking(() => other.ApplyTo(Project(), Now)).Should().Throw<InvalidOperationException>();
        FluentActions.Invoking(() => ProofFeedback.Parse("""{ "format": "baska" }""")).Should().Throw<FormatException>();
        FluentActions.Invoking(() => ProofFeedback.Parse("""{ "format": "nexalbum-proof-feedback" }""")).Should().Throw<FormatException>();
    }

    [Fact]
    public void LongOrControlCharacterNotesAreCleaned()
    {
        var note = new string('a', 2500) + "\\u0007";
        var feedback = ProofFeedback.Parse(Feedback(Id, $$"""{ "id": "page-002", "liked": false, "note": "{{note}}" }"""));

        feedback.Pages.Single().Note.Should().HaveLength(ProofFeedback.MaxNote).And.NotContain("\u0007");
    }
}
