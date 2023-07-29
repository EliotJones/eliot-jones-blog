using LightBlog.Server.Models.Posts;
using Microsoft.AspNetCore.Mvc;

namespace LightBlog.Server.ViewComponents;

public class SideBarViewComponent : ViewComponent
{
    private readonly IPostRepository postRepository;

    public SideBarViewComponent(IPostRepository postRepository)
    {
        this.postRepository = postRepository;
    }

    public Task<IViewComponentResult> InvokeAsync()
    {
        var topPosts = postRepository.GetMostRecent(10);

        return Task.FromResult((IViewComponentResult)View(topPosts));
    }
}