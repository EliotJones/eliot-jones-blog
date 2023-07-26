using LightBlog.Server.Models.Posts;
using Microsoft.AspNetCore.Mvc;

namespace LightBlog.Server.Controllers;

public class PostController : Controller
{
    private readonly IPostRepository postRepository;
    private readonly ILogger<PostController> logger;

    public PostController(IPostRepository postRepository,
        ILogger<PostController> logger)
    {
        this.postRepository = postRepository;
        this.logger = logger;
    }

    public IActionResult Index(int year, int month, string name, [FromQuery] bool comments = false)
    {
        logger.LogInformation("Getting post year {year}, month {month}, name: {name}",
            year,
            month,
            name);

        if (!postRepository.TryFindPost(year, month, name, out var model))
        {
            logger.LogInformation("Post not found!");

            return RedirectToAction("Index", "Home");
        }

        ViewData["comments"] = comments;

        return View(model);
    }
}