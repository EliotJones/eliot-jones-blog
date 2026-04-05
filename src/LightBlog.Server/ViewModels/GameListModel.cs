namespace LightBlog.Server.ViewModels;

public record GameListModel
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string JsUrl { get; init; }

    public required string Description { get; init; }

    public required DateOnly Date { get; init; }

    public required IReadOnlyList<AssetCredit> Credits { get; init; }
}

public record AssetCredit(string Name, string Url);