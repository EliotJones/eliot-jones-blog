using LightBlog.Server.Models.Posts;
using LightBlog.Server.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace LightBlog.Server.Controllers;

public class HomeController : Controller
{
    private readonly IPostRepository postRepository;
    private readonly List<GameListModel> games = new List<GameListModel>
        {
            new GameListModel
            {
                Date = new DateOnly(2025, 8, 9),
                Name = "Phaser Tutorial",
                Description = "Modified code from the Phaser 3 tutorial game.",
                Id = "phaser",
                JsUrl = "phaser-tutorial-20250809"
            }
        };

    public HomeController (IPostRepository postRepository)
    {
        this.postRepository = postRepository;
    }

    public IActionResult Index(int page = 1)
    {
        var posts = postRepository.GetPaged(page, 5);

        return View(posts);
    }
    
    [HttpGet("games")]
    public IActionResult Games()
    {
        return View("GameList", games);
    }

    [HttpGet("games/{gameId}")]
    public IActionResult Game(string gameId)
    {
        var game = games.FirstOrDefault(x => string.Equals(x.Id, gameId, StringComparison.OrdinalIgnoreCase));

        if (game == null)
        {
            return NotFound();
        }

        return View("Game", game);
    }

    public IActionResult Error()
    {
        return View();
    }
}