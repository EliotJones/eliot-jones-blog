namespace LightBlog.Server.Models;

public class SiteOptions
{
    public string Name { get; set; } = "My Site";

    public string Description { get; set; } = string.Empty;

    public bool SummaryOnly { get; set; }

    public string AuthorName { get; set; } = "Author";
}