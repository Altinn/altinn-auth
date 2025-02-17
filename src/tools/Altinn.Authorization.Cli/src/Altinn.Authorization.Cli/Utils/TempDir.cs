namespace Altinn.Authorization.Cli.Utils;

/// <summary>
/// A temporary directory that can be deleted when disposed.
/// </summary>
internal sealed class TempDir
    : IDisposable
{
    private readonly bool _deleteOnDispose;
    private readonly DirectoryInfo _dir;

    /// <summary>
    /// Creates a new temporary directory.
    /// </summary>
    /// <param name="deleteOnDispose">Whether to delete the directory when the <see cref="TempDir"/> is disposed.</param>
    /// <returns>A <see cref="TempDir"/>.</returns>
    public static TempDir Create(bool deleteOnDispose = true)
    {
        var dir = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
        dir.Create();

        return new TempDir(dir, deleteOnDispose);
    }

    private TempDir(DirectoryInfo dir, bool deleteOnDispose)
    {
        _dir = dir;
        _deleteOnDispose = deleteOnDispose;
    }

    public string DirPath => _dir.FullName;

    /// <inheritdoc cref="File.CreateText(string)"/>
    public StreamWriter CreateText(string path)
        => File.CreateText(EnsureFilePath(path));

    /// <inheritdoc cref="File.OpenText(string)"/>
    public StreamReader OpenText(string path)
        => File.OpenText(EnsureFilePath(path));

    private string EnsureFilePath(string relPath)
    {
        var path = Path.Combine(_dir.FullName, relPath);
        var dir = Path.GetDirectoryName(path);
        Directory.CreateDirectory(dir!);

        return path;
    }

    /// <summary>
    /// Deletes the temporary directory and all its contents.
    /// </summary>
    public void Delete()
    {
        _dir.Delete(recursive: true);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_deleteOnDispose)
        {
            Delete();
        }
    }
}
