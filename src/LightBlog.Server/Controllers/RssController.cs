using System.Text;
using LightBlog.Server.Models;
using Microsoft.AspNetCore.Mvc;

namespace LightBlog.Server.Controllers;

public class RssController : Controller
{
    private readonly ILogger<RssController> logger;
    private readonly IRssFeedFactory feedFactory;

    public RssController(IRssFeedFactory feedFactory,
        ILogger<RssController> logger)
    {
        this.logger = logger;
        this.feedFactory = feedFactory;
    }

    public IActionResult Index()
    {
        logger.LogInformation("Getting RSS feed.");

        var feed = feedFactory.GetFeed();

        return Content(feed, "text/xml", Encoding.UTF8);
    }
}