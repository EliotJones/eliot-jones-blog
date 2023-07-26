using LightBlog.Server.Models;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using LightBlog.Server.Models.Posts;
using Microsoft.Extensions.Caching.Memory;

namespace LightBlog.Server;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var services = builder.Services;

        services.AddOptions();

        builder.Services.Configure<SiteOptions>(
            builder.Configuration.GetSection("SiteOptions"));

        services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
        
        services.AddSingleton<IPostRepository, PostRepository>(x => new PostRepository(
            x.GetRequiredService<IWebHostEnvironment>().WebRootPath,
            x.GetRequiredService<IMemoryCache>(),
            x.GetRequiredService<ILogger<PostRepository>>()));

        services.AddTransient<IRssFeedFactory, RssFeedFactory>();
        
        builder.Services.AddControllersWithViews();

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
        }

        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.MapControllerRoute(
            name: "post",
            pattern: "{year:int}/{month:int}/{name}",
            defaults: new { controller = "Post", action = "Index" });

        app.Run();
    }
}