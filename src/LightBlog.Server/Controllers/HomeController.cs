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
                JsUrl = "phaser-tutorial-20250809",
                Credits = [
                    new ("Sprite and bombs", "https://phaser.io/tutorials/making-your-first-phaser-3-game/part1"),
                    new ("Background", "https://free-game-assets.itch.io/free-sky-with-clouds-background-pixel-art-set"),
                    new ("Tileset", "https://rottingpixels.itch.io/four-seasons-platformer-tileset-16x16free"),
                    new ("Pause button", "https://kicked-in-teeth.itch.io/button-ui"),
                    new ("Coin sound", "https://pixabay.com/users/liecio-3298866/?utm_source=link-attribution&utm_medium=referral&utm_campaign=music&utm_content=190037"),
                    new ("Game over sound", "https://pixabay.com/sound-effects/080047-lose-funny-retro-video-game-80925/"),
                    new ("Background music", "https://pixabay.com/sound-effects/be-more-serious-loop-275528/")
                ],
            },
            new GameListModel
            {
                Date = new DateOnly(2026, 4, 5),
                Name = "Sidescroll Platformer",
                Description = "Tried building a platformer but I got bored",
                Id = "sidescroll-plat",
                JsUrl = "sidescroll-platformer-20260405",
                Credits = [
                    new ("Fox and FX", "https://ansimuz.itch.io/sunny-land-pixel-game-art"),
                    new ("Background", "https://aethrall.itch.io/demon-woods-parallax-background"),
                    new ("Tileset", "https://rottingpixels.itch.io/four-seasons-platformer-tileset-16x16free"),
                    new ("Pickup sound", "https://pixabay.com/users/alexis_gaming_cam-50011695/?utm_source=link-attribution&utm_medium=referral&utm_campaign=music&utm_content=367087"),
                    new ("Powerup sound", "https://pixabay.com/users/ribhavagrawal-39286533/?utm_source=link-attribution&utm_medium=referral&utm_campaign=music&utm_content=230548"),
                    new ("Coin sound", "https://pixabay.com/users/liecio-3298866/?utm_source=link-attribution&utm_medium=referral&utm_campaign=music&utm_content=190037"),
                    new ("Game over sound", "https://pixabay.com/sound-effects/080047-lose-funny-retro-video-game-80925/"),
                    new ("Background music", "https://pixabay.com/sound-effects/be-more-serious-loop-275528/")
                ],
            }
        };

    public HomeController(IPostRepository postRepository)
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