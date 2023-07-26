# ASP.NET Core Identity Using PostgreSQL #

Following on from my much older posts about using ASP.NET Identity 2 to manage user accounts in MVC 4 sites, today I needed to use Identity on an ASP.NET Core MVC site.

As with previous versions, the current [Identity library](https://github.com/aspnet/Identity "GitHub for ASP.NET Core Identity") for .NET Core 1.1 uses Entity Framework out-the-box. Luckily it's much easier to change this behaviour for the simplest register/log-in flow.

### Install ###

Firstly you need to get the right NuGet package. For the Identity library without Entity Framework this is:

```Microsoft.AspNetCore.Identity```

Once this is installed in your project you need to provide your own implementation of the ```IUserStore<TUser>``` and ```IRoleStore<TRole>```.

Unlike previous versions the ```TUser``` no longer needs to implement a specific interface.

For my project I was using PostgreSQL as the database and Dapper.Contrib as the ORM. My User class was simply:

    [Table("\"user\"")]
    public class User
    {
        [ExplicitKey]
        public Guid Id { get; set; }

        public string UserName { get; set; }

        public string NormalizedUserName { get; set; }

        public string Email { get; set; }

        public string PasswordHash { get; set; }
    }

(I had to do some faffing around to make sure the table name was detected correctly in my schema but could have called the table anything, it doesn't have to be called "user").

You also need a class for Roles, since I won't be implementing Roles in this tutorial I just created a blank class:

    public class Role { }

### Fun In Store ###

Now we need to implement a couple of interfaces.

For users I wanted to support password based log-in. 

By default you only need to provide the ```IUserStore<TUser>``` implementation, however for almost all applications the behaviours supported by the ```IUserPasswordStore<TUser>``` are desired. The ```IUserPasswordStore<TUser>``` interface implements ```IUserStore<TUser>```:

    public class UserPasswordStore : IUserPasswordStore<User>
    {
        private readonly IDataContext context;

        public UserPasswordStore(IDataContext context)
        {
            this.context = context;
        }

        public async Task<IdentityResult> CreateAsync(User user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            await context.CreateAsync(user);
            
            return IdentityResult.Success;
        }

        public async Task<IdentityResult> DeleteAsync(User user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            await context.DeleteAsync(user);

            return IdentityResult.Success;
        }

        public void Dispose()
        {
        }

        public async Task<User> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Guid id;
            if (!Guid.TryParse(userId, out id))
            {
                throw new ArgumentException("Id was not a valid Guid: " + userId, nameof(userId));
            }

            return await context.GetByIdAsync<User>(id);
        }

        public async Task<User> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var users = await context.GetAllAsync<User>();

            return users.FirstOrDefault(x => string.Equals(normalizedUserName, x.NormalizedUserName, StringComparison.OrdinalIgnoreCase));
        }
        
        public Task<string> GetNormalizedUserNameAsync(User user, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(user.NormalizedUserName);
        }

        public Task<string> GetPasswordHashAsync(User user, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(user.PasswordHash);
        }

        public Task<string> GetUserIdAsync(User user, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(user.Id.ToString());
        }

        public Task<string> GetUserNameAsync(User user, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(user.UserName);
        }

        public Task<bool> HasPasswordAsync(User user, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task SetNormalizedUserNameAsync(User user, string normalizedName, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            user.NormalizedUserName = normalizedName;

            return Task.CompletedTask;
        }

        public Task SetPasswordHashAsync(User user, string passwordHash, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            user.PasswordHash = passwordHash;

            return Task.CompletedTask;
        }

        public Task SetUserNameAsync(User user, string userName, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            user.UserName = userName;

            return Task.CompletedTask;
        }

        public async Task<IdentityResult> UpdateAsync(User user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            await context.UpdateAsync(user);

            return IdentityResult.Success;
        }
    }

A lot of this code is boilerplate to check for cancellation tokens and to verify arguments, the underlying logic for data access is very basic.

I have not yet implemented the lock-out code in the sample above but it will be fairly simple and similar to the existing code. Just implement ```IUserLockoutStore<T>``` in addition to ```IUserPasswordStore<T>```.

The ```IDataContext``` class used here is just my custom injectable wrapper around the Dapper/PostgreSQL connection, if you wanted you could directly connect to the database or use whichever data access library you want.

My data context class uses Dapper.Contrib to perform simple CRUD operations:

    public class DapperDataContext : IDataContext, IDisposable
    {
        private readonly IDbConnection connection;

        public DapperDataContext(IOptions<DatabaseOptions> databaseOptions)
        {
            connection = new NpgsqlConnection(databaseOptions.Value.ConnectionString);
        }

        public async Task CreateAsync<T>(T item) where T : class
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            await connection.InsertAsync(item);
        }

        public async Task DeleteAsync<T>(T item) where T : class
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            await connection.DeleteAsync(item);
        }

        public async Task<T> GetByIdAsync<T>(Guid id) where T : class
        {
            return await connection.GetAsync<T>(id);
        }

        public void Dispose()
        {
            connection.Dispose();
        }

        public async Task<IReadOnlyCollection<T>> GetAllAsync<T>() where T : class
        {
            var items = await connection.GetAllAsync<T>();

            var result = new List<T>();
            result.AddRange(items);

            return result;
        }

        public async Task UpdateAsync<T>(T item) where T : class
        {
            await connection.UpdateAsync(item);
        }
    }

You also need to provide an implementation of the ```IRoleStore<TRole>``` which for this demo I am ignoring and leaving the methods not implemented. These methods will not be called for our default code, except for the dispose method. For this reason remember to remove the ```NotImplementedException``` from the ```Dispose``` method:

    public class RoleStore : IRoleStore<Role>
    {
        public Task<IdentityResult> CreateAsync(Role role, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IdentityResult> DeleteAsync(Role role, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
        }

        public Task<Role> FindByIdAsync(string roleId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<Role> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetNormalizedRoleNameAsync(Role role, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetRoleIdAsync(Role role, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetRoleNameAsync(Role role, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task SetNormalizedRoleNameAsync(Role role, string normalizedName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task SetRoleNameAsync(Role role, string roleName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IdentityResult> UpdateAsync(Role role, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

### Register Services ###

Now all that's left to do is register the services at the start up of your MVC or Web API application.

In the ```ConfigureServices``` method of the ```Startup.cs``` class register your user, role, user store and role store classes as well as any injectable services required by your code.

To register the identity classes correctly, use the ```AddIdentity<TUser, TRole>``` extension method. Then register both the user and role store. These method calls must be chained off/follow the call to ```AddIdentity```:

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddIdentity<User, Role>()
            .AddRoleStore<RoleStore>()
            .AddUserStore<UserPasswordStore>()
            .AddDefaultTokenProviders();

        services.AddMvc();

        // Add application services.
        services.AddTransient<IDataContext, DapperDataContext>();

        services.Configure<DatabaseOptions>(Configuration.GetSection("Database"));
    }

### Calling The Store ###

Once the services are registered you can request an instance ```UserManager<User>``` and/or ```SignInManager<User>``` in your controller's constructor. The Identity and Dependency Injection code will handle resolving and registering these based off the store implementations we provided. These then support registration and log-in methods. The log-in method is shown in the controller below:

    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public AccountController(UserManager<User> userManager, SignInManager<User> signInManager,)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    return RedirectToLocal(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToAction(nameof(SendCode), new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    return View("Lockout");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }
    }

To easily get controller templates with the usual set of registration and log-in methods supported, simply create a new MVC project from the Visual Studio 2017 template with the Individual Accounts option enabled. Then remove the Entity Framework based Identity library and related classes (such as ```ApplicationUser```) and instead implement the code from this guide.

This gets you the simplest working log-in and registration system, but there's a lot left to implement including the lockout store and the role store as well as 3rd party authentication providers depending on the requirements of your application.

I've put a simple version of this code which uses text files to store data [on GitHub](https://github.com/EliotJones/AspIdentityCoreDemo "Code on GitHub").
