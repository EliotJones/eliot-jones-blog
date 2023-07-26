using LightBlog.Server.ViewModels;

namespace LightBlog.Server.Models.Posts;

public class PostFindResult
{
    public PostViewModel Post { get; }

    public bool IsFound { get; }

    public PostFindResult (PostViewModel post)
    {
        Post = post;
        IsFound = post != null;
    }
}