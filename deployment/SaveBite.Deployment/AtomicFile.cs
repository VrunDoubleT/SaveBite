using System.Text;

namespace SaveBite.Deployment;

internal static class AtomicFile
{
    public static async Task WriteAsync(string path, string content, bool privateFile, CancellationToken ct)
    {
        path = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write };
            if (!OperatingSystem.IsWindows())
                options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite
                    | (privateFile ? 0 : UnixFileMode.GroupRead | UnixFileMode.OtherRead);
            await using (var stream = new FileStream(temporary, options))
            await using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                await writer.WriteAsync(content.AsMemory(), ct);
                await writer.FlushAsync(ct);
            }
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            File.Delete(temporary);
        }
    }
}
