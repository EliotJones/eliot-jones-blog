# Blog Update

As this blog entered its tenth year it was desperately in need of a facelift. I had effectively stopped writing new posts because the overhead of remembering how to upload a new post each time was off-putting enough to stop me entirely.

For that reason it has been updated with new - hopefully more consistent - CSS. In addition the code and posts are deployed on each commit by GitHub actions. This means I no longer need to SSH into the server to do things by hand. This should mean the blog is editable from wherever I have git access. I also removed the terribly insecure previous method of uploading posts directly onto the server which was using a plain-text username and password.

Most importantly privacy-invading Google Analytics has been removed along with almost all JavaScript. I no longer see the need for analytics on this blog. The only remaining scripts are for code highlighting client-side as well as Disqus for commenting. Disqus is opt-in so will only load in if you click the "Load Comments" button at the bottom of each post.

Unfortunately I still rely on Google Fonts for the title font but the main body font has been updated to use the system font stack.

Finally it has been updated to .NET 7. [The previous version](https://github.com/EliotJones/LightBlog/blob/release/EliotJones/LightBlog.csproj) was running .NET Core 1.1 and hadn't been updated since 2017. So much for keeping software up-to-date! [The new version](https://github.com/EliotJones/eliot-jones-blog/blob/main/src/LightBlog.Server/LightBlog.Server.csproj) is on .NET 7 and the upgrade process was surprisingly easy.

While the changes between the ASP .NET versions have been a little hard to keep track of with `Startup.cs` being removed and everything moving into `Program.cs` the migration was made simple by my lazy cheat. I just created a new ASP .NET 7 application from the template in Visual Studio and moved most of the files without any changes, except to update the namespaces.

I also took the opportunity to add caching. Previously it loaded every post file from disk repeatedly just to show a single page. Now, because posts are only updated whenever the app itself is deployed and restarted, cache invalidation is trivial.

I have added the images into git too which generally causes a lot of squawking from people who use git properly (nerds), but keeping things simple should hopefully mean I fall into the pit of success. Each PNG image has been compressed further where possible.

### Deployment

Deployment of the blog is pretty much unchanged from the initial set-up following [the guide by Scott Hanselman](https://www.hanselman.com/blog/publishing-an-aspnet-core-website-to-a-cheap-linux-vm-host) for deploying .NET to a Linux server such as a DigitalOcean VPS. This may no longer be best-practice but has been working for me uninterrupted for over half a decade.

My DigitalOcean VPS is the cheapest tier ($5 a month originally, now $6-something thanks to inflation) and hosts this blog plus a couple of other applications.

The application is published from either my Windows machine or the GitHub CI runner using:

```
dotnet publish -c Release -r linux-x64
```

Which generates a self-contained application (when `<SelfContained>` is defined in the .csproj file). The application is then copied to the server (not directly to the deployed directory since this is locked by the running app) using scp:

```
scp -r /c/path/to/publish username@ip-or-host:/path/on/remote
```

Each application is deployed to the `/var/{app-folder}` directory on the server and launched and managed by [Supervisor](http://supervisord.org/).

The Supervisor config files live at `/etc/supervisor/conf.d`. Each file defines an application for Supervisor to manage. This blog lives under `/var/blog`:

```
[program:blog]
command=/usr/bin/dotnet /var/blog/LightBlog.Server.dll --server.urls:http://*:5000
directory=/var/blog
autostart=true
autorestart=true
stderr_logfile=/var/log/blog.err.log
stdout_logfile=/var/log/blog.out.log
environment=ASPNETCORE_ENVIRONMENT=Production
user=www-data
stopsignal=INT
```

To copy the newly `scp` published app to the `/var/{app-folder}` directory the running application (named blog) is first stopped using:

```
supervisorctl stop blog
```

The existing files are then cleared, the newly uploaded files copied to the target folder and finally the blog restarted using:

```
supervisorctl start blog
```

There are more details in Scott's blog-post for configuring NGINX as a reverse proxy and using Let's Encrypt to expose the site over HTTPS.

The [GitHub actions workflow for deployments is here](https://github.com/EliotJones/eliot-jones-blog/blob/main/.github/workflows/build-and-deploy.yml).

Well that's all for this update. I'm hoping to write a post about building stupid code for fun [to explain FAF Lobby Sim](http://eliot-jones.com:6575/) (warning, not HTTPS!) soon, but based on my track record see you in 2030...
