namespace NexAlbum.Architecture.Tests;

internal static class RepoRoot
{
    private const string SolutionFile = "NexAlbum.slnx";

    public static string Find()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, SolutionFile)))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException($"{SolutionFile} bulunamadı.");
    }
}
