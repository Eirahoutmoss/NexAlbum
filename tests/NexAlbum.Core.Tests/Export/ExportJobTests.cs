using FluentAssertions;
using NexAlbum.Core.Export;
using Xunit;

namespace NexAlbum.Core.Tests.Export;

public class ExportJobTests
{
    private static ExportJob NewJob() => new()
    {
        Id = "export-001",
        Format = ExportFormat.Pdf,
        TargetPath = @"C:\album.pdf",
        ProfileId = "p",
    };

    [Fact]
    public void HappyPathTransitions()
    {
        var start = TestData.Now;
        var end = start.AddSeconds(45);
        var job = NewJob().Start(start).Complete(end, [@"C:\album.pdf"]);

        job.Status.Should().Be(ExportStatus.Completed);
        job.StartedAt.Should().Be(start);
        job.CompletedAt.Should().Be(end);
        job.OutputFiles.Should().Equal(@"C:\album.pdf");
        job.IsFinished.Should().BeTrue();
    }

    [Fact]
    public void FailureKeepsTheError()
    {
        var job = NewJob().Start(TestData.Now).Fail(TestData.Now, "Disk dolu");
        job.Status.Should().Be(ExportStatus.Failed);
        job.Error.Should().Be("Disk dolu");
    }

    [Fact]
    public void InvalidTransitionsAreRejected()
    {
        FluentActions.Invoking(() => NewJob().Complete(TestData.Now, [])).Should().Throw<InvalidOperationException>();
        FluentActions.Invoking(() => NewJob().Start(TestData.Now).Start(TestData.Now)).Should().Throw<InvalidOperationException>();
        var done = NewJob().Start(TestData.Now).Complete(TestData.Now, []);
        FluentActions.Invoking(() => done.Cancel(TestData.Now)).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void PendingOrRunningJobsCanBeCancelled()
    {
        NewJob().Cancel(TestData.Now).Status.Should().Be(ExportStatus.Cancelled);
        NewJob().Start(TestData.Now).Cancel(TestData.Now).IsFinished.Should().BeTrue();
    }

    [Fact]
    public void OptionsHaveSafeDefaultsAndValidate()
    {
        var options = new ExportOptions();
        options.Pages.Should().Be("all");
        options.IncludeBleed.Should().BeTrue();
        options.IncludeCropMarks.Should().BeTrue();
        options.ColorProfile.Should().BeNull("ICC profili uydurulmaz");
        FluentActions.Invoking(() => options with { JpegQuality = 101 }).Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => options with { Pages = "" }).Should().Throw<ArgumentException>();
    }
}
