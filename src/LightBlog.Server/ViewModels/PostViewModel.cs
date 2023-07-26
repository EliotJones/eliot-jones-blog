namespace LightBlog.Server.ViewModels;

public class PostViewModel
{
    public required DateTime Date { get; init; }

    public required string RawHtml { get; init; }

    public required string RawHtmlSummary { get; init; }

    public required string Title { get; init; }

    public required int Year { get; init; }

    public required int Month { get; init; }

    public required string Name { get; init; }
}