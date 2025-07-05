using EkofyApp.Domain.Enums;

namespace EkofyApp.Domain.Utils;
public class PathHelper
{
    public static string ResolvePath(PathTag tag, params string[] more)
    {
        string baseDir = AppContext.BaseDirectory;

        string result = tag switch
        {
            PathTag.Bin => baseDir,
            PathTag.Base => Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..")),
            PathTag.Api => Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..")),
            PathTag.Tools => Path.GetFullPath(Path.Combine(Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..")), "Tools")),
            PathTag.PrivateKeys => Path.GetFullPath(Path.Combine(Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..")), "PrivateKeys")),
            _ => throw new ArgumentOutOfRangeException(nameof(tag), tag, null)
        };

        if (more != null && more.Length > 0)
        {
            result = Path.Combine(new[] { result }.Concat(more).ToArray());
        }

        return result;
    }
}
