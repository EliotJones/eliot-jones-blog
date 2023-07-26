using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using LightBlog.Server.ViewModels;
using MarkdownSharp;
using Microsoft.Extensions.Caching.Memory;

namespace LightBlog.Server.Models.Posts;

public interface IPostRepository
{
    IReadOnlyList<PostViewModel> GetAll();

    PagedPostsViewModel GetPaged(int page, int pageSize);

    bool TryFindPost(int year, int month, string name, [NotNullWhen(true)] out PostViewModel? post);

    IReadOnlyList<PostViewModel> GetTopPosts(int number);
}

public partial class PostRepository : IPostRepository
{
    // Summon Cthulhu
    [GeneratedRegex("<h1>.+</h1>", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex TitleRegex();

    private readonly IMemoryCache memoryCache;
    private readonly ILogger<PostRepository> logger;
    private readonly Lazy<IReadOnlyList<PostInformation>> postsInfo;

    public PostRepository (
        string webRootPath,
        IMemoryCache memoryCache,
        ILogger<PostRepository> logger)
    {
        this.memoryCache = memoryCache;
        this.logger = logger;
        this.postsInfo = new Lazy<IReadOnlyList<PostInformation>>(() => GetPostsInitial(webRootPath));
    }

    public PagedPostsViewModel GetPaged(int page, int pageSize)
    {
        logger.LogInformation("Getting posts for page {page}", page);

        var fileInformation = GetAllPostInformation();

        var posts = fileInformation.OrderByDescending(x => x.Date)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ViewModelFactory)
            .ToList();

        return new PagedPostsViewModel(posts, page, 
            (int)Math.Ceiling(fileInformation.Count / (double)pageSize));
    }

    public IReadOnlyList<PostViewModel> GetAll()
    {
        var fileInformation = GetAllPostInformation();

        return fileInformation.Select(ViewModelFactory).ToList();
    }

    private IReadOnlyList<PostInformation> GetAllPostInformation() => postsInfo.Value;

    public bool TryFindPost(int year, int month, string name, [NotNullWhen(true)] out PostViewModel? post)
    {
        post = null;

        var posts = GetAllPostInformation();

        var matchingPostInfo = posts
            .FirstOrDefault(x => x.Date.Year == year
                                 && x.Date.Month == month
                                 && x.Name.Contains(name, StringComparison.OrdinalIgnoreCase));

        if (matchingPostInfo == null)
        {
            return false;
        }

        post = ViewModelFactory(matchingPostInfo);

        return true;
    }

    public IReadOnlyList<PostViewModel> GetTopPosts(int number)
    {
        var posts = GetAllPostInformation();

        return posts.OrderByDescending(x => x.Date).Take(number)
            .Select(ViewModelFactory)
            .ToList();
    }

    private static IReadOnlyList<PostInformation> GetPostsInitial(string webRootDirectory)
    {
        var postsDirectory = Path.Combine(webRootDirectory, "posts");

        var result = new List<PostInformation>();
        foreach (var file in Directory.EnumerateFiles(postsDirectory, "*.md", SearchOption.TopDirectoryOnly))
        {
            if (!PostInformation.TryParsePostInformation(file, out var info))
            {
                continue;
            }

            result.Add(info);
        }

        return result;
    }

    private PostViewModel ViewModelFactory(PostInformation postInformation)
    {
        var cached = memoryCache.Get<PostViewModel>(postInformation.Name);

        if (cached != null)
        {
            return cached;
        }

        var date = postInformation.Date;
        var markdown = File.ReadAllText(postInformation.FilePath);

        var converter = new Markdown();

        var text = converter.Transform(markdown);

        var regexMatch = TitleRegex().Match(text);

        var title = regexMatch.Value.Replace("<h1>", string.Empty)
            .Replace("</h1>", string.Empty);

        if (!string.IsNullOrEmpty(regexMatch.Value))
        {
            text = text.Replace(regexMatch.Value, string.Empty);
        }

        var year = postInformation.Date.Year;
        var month = postInformation.Date.Month;

        var lastUnderscoreIndex = postInformation.Name.LastIndexOf("_", StringComparison.OrdinalIgnoreCase);
        var name = postInformation.Name[..lastUnderscoreIndex];

        var summary = GetSummary(text);

        var result = new PostViewModel
        {
            Date = date,
            Month = month,
            Name = name,
            RawHtmlSummary = summary,
            RawHtml = text,
            Title = title,
            Year = year
        };

        var cacheOpts = new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(10)
        };

        memoryCache.Set(postInformation.Name, result, cacheOpts);

        return result;
    }

    private static string GetSummary(string fullText)
    {
        var paragraphs = fullText.Split(new[] { "</p>" }, StringSplitOptions.RemoveEmptyEntries).ToList();

        var firstImageParagraph = paragraphs.FirstOrDefault(x => x.Contains("<img"));

        int index;
        if (firstImageParagraph != null)
        {
            index = paragraphs.IndexOf(firstImageParagraph);

            if (index > 6)
            {
                index = 6;
            }

            index = Math.Max(index, 3);
        }
        else
        {
            index = Math.Min(7, paragraphs.Count - 1);
        }

        return string.Join(string.Empty, paragraphs.Take(index + 1));
    }
}