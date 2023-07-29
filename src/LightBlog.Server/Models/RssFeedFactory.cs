using LightBlog.Server.Models.Posts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Options;
using WilderMinds.RssSyndication;

namespace LightBlog.Server.Models;

internal class RssFeedFactory : IRssFeedFactory
{
    private readonly IPostRepository postRepository;
    private readonly IUrlHelper urlHelper;
    private readonly ILogger<RssFeedFactory> logger;
    private readonly IOptions<SiteOptions> siteOptions;

    public RssFeedFactory(IPostRepository postRepository,
        IUrlHelperFactory urlHelperFactory,
        IActionContextAccessor actionContextAccessor,
        ILogger<RssFeedFactory> logger,
        IOptions<SiteOptions> siteOptions)
    {
        this.postRepository = postRepository;
        this.urlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext!);
        this.logger = logger;
        this.siteOptions = siteOptions;
    }

    public string GetFeed()
    {
        var posts = postRepository.GetAll();

        var link = urlHelper.Link("default", new { action = "Index", controller = "Home" });

        if (link == null)
        {
            return string.Empty;
        }

        var feed = new Feed
        {
            Title = siteOptions.Value.Name,
            Description = siteOptions.Value.Description,
            Link = new Uri(link)
        };

        var result = new List<Item>(posts.Count);
        foreach (var x in posts.OrderByDescending(x => x.Date))
        {
            var url = urlHelper.Link("post", new { year = x.Year, month = x.Month, name = x.Name });

            if (url == null)
            {
                continue;
            }

            result.Add(new Item
            {
                Title = x.Title,
                Body = x.RawHtml,
                Link = new Uri(url),
                Permalink = url,
                PublishDate = x.Date,
                Author = new Author { Email = siteOptions.Value.AuthorName, Name = siteOptions.Value.Name }
            });
        }

        logger.LogInformation($"Found {result.Count} posts");

        feed.Items = result;

        var rss = feed.Serialize();

        return rss;
    }
}

public interface IRssFeedFactory
{
    string GetFeed();
}