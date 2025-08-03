namespace LightBlog.Server.ViewModels;

public class PostViewModel
{
    public required DateOnly Date { get; init; }

    public required string RawHtml { get; init; }

    public required string RawHtmlSummary { get; init; }

    public required string Title { get; init; }

    public required string Name { get; init; }
}