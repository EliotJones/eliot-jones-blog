using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace LightBlog.Server.Models.Posts;

public class PostInformation
{
    public string FilePath { get; }

    public string Name { get; }

    public DateOnly Date { get; }

    private PostInformation(string filePath, string name, DateOnly date)
    {
        FilePath = filePath;
        Name = name;
        Date = date;
    }

    public static bool TryParsePostInformation(string filePath, [NotNullWhen(true)] out PostInformation? postInformation)
    {
        postInformation = null;

        var lastUnderscoreIndex = filePath.LastIndexOf("_", StringComparison.OrdinalIgnoreCase);
        var datePart = filePath[(lastUnderscoreIndex + 1)..].Replace(".md", string.Empty);

        if (!DateOnly.TryParseExact(datePart, "ddMMyyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return false;
        }

        var name = Path.GetFileName(filePath);

        postInformation = new PostInformation(filePath, name, date);

        return true;
    }
}