namespace LightBlog.Server.ViewModels;

public class PagedPostsViewModel
{
    public IReadOnlyList<PostViewModel> Posts { get; set; }

    public int PageNumber { get; set; }

    public int TotalPages { get; set; }

    public PagedPostsViewModel (IReadOnlyList<PostViewModel> posts, 
        int pageNumber, 
        int totalPages)
    {
        Posts = posts;
        PageNumber = pageNumber;
        TotalPages = totalPages;
    }
}