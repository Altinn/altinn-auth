namespace Altinn.Authorization.Cli.Utils;

/// <summary>
/// A temporary directory which can be automatically deleted when disposed.
/// </summary>
internal sealed class TempDir
    : IAsyncDisposable
{
    public static TempDir Create(bool delete = true)
    {
        var dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
        
        return new TempDir(dir, delete);
    }

    private readonly DirectoryInfo _dir;
    private readonly bool _delete;

    private TempDir(DirectoryInfo dir, bool delete)
    {
        _dir = dir;
        _delete = delete;
    }

    public FileInfo File(string name)
    {
        return new FileInfo(Path.Combine(_dir.FullName, name));
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        if (_delete && _dir.Exists)
        {
            _dir.Delete(recursive: true);
        }

        return ValueTask.CompletedTask;
    }
}
