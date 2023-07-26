# ASP.NET Identity 2.0 Tutorial - EF Free (Post 2)#

*Note: This is the second post in a 2 post tutorial on implementing Identity 2.0 without using EF, for post 1 go [here][link0].*

Now we have our User and UserStore classes we can change the UserManager and SignInManager our application uses. These classes are in the ```App_Start/IdentityConfig.cs``` file.

I split the classes out to their own files, ```ApplicationUserManager.cs``` and ```ApplicationSignInManager.cs``` respectively. In their unmodified state these classes inherit from the classes provided by the Identity library.

### ApplicationUserManager
The manager provides many methods to use in our controllers, a few examples are:

	public virtual Task<IdentityResult> ResetAccessFailedCountAsync(TKey userId);
	public virtual Task<IdentityResult> ResetPasswordAsync(TKey userId, string token, string newPassword);
	public virtual Task SendEmailAsync(TKey userId, string subject, string body);

The manager mainly delegates to classes it owns such as the user store to run these methods. The default template UserManager is shown below:

	public class ApplicationUserManager : UserManager<ApplicationUser>
	{
	    public ApplicationUserManager(IUserStore<ApplicationUser> store)
	        : base(store) { }
	
	    public static ApplicationUserManager Create(IdentityFactoryOptions<ApplicationUserManager> options, 
	        IOwinContext context) 
	    {
	        var manager = new ApplicationUserManager(
	            new UserStore<ApplicationUser>(context.Get<ApplicationDbContext>()));

	        // Configure validation logic for usernames
	        manager.UserValidator = new UserValidator<ApplicationUser>(manager)
	        {
	            AllowOnlyAlphanumericUserNames = false,
	            RequireUniqueEmail = true
	        };
	
	        // Configure validation logic for passwords
	        manager.PasswordValidator = new PasswordValidator
	        {
	            RequiredLength = 6,
	            RequireNonLetterOrDigit = true,
	            RequireDigit = true,
	            RequireLowercase = true,
	            RequireUppercase = true,
	        };

	        [CONTENT REMOVED TO SAVE SPACE...]
	        return manager;
	    }
	}

Because everything takes arguments based on interfaces we've already created our classes for, the rewrite is very simple. Firstly we replace all instances of ```ApplicationUser``` with our class which implements IUser, in our case ```MyUser```:

	public class ApplicationUserManager : UserManager<MyUser>
	{
	    public ApplicationUserManager(IUserStore<MyUser> store)
	        : base(store)
	    {
	        this.UserValidator = new MyUserValidator<MyUser, string>();
	    }
	
	    public static ApplicationUserManager Create(IdentityFactoryOptions<ApplicationUserManager> options, 
	        IOwinContext context)
	    {
	        var manager = new ApplicationUserManager(new MyUserStore<MyUser>());
	        // Configure validation logic for usernames
	        manager.UserValidator = new UserValidator<MyUser>(manager)
	        {
	            AllowOnlyAlphanumericUserNames = false,
	            RequireUniqueEmail = true
	        };
	
	        // Configure validation logic for passwords
	        manager.PasswordValidator = new PasswordValidator
	        {
	            RequiredLength = 3,
	            RequireNonLetterOrDigit = false,
	            RequireDigit = false,
	            RequireLowercase = true,
	            RequireUppercase = false
	        };
	
	        // Configure user lockout defaults
	        manager.UserLockoutEnabledByDefault = false;
	
	        manager.EmailService = new EmailService();
	        manager.SmsService = new SmsService();
	
	        return manager;
	    }
	}

I set the password requirements to be super weak for manual testing because typing secure passwords repeatedly was too much like hard work!

We also remove the default ```UserStore``` which depends on Entity Framework and insert our own ```MyUserStore``` (which in this tutorial also depends on EF but with no requirement to pass a DbContext to the constructor). Additionally all parts setting up 2 Factor Auth are removed.

An extra change in the Setup method is required, without it the call to UserManager ```CreateAsync(user, password)``` may fail when it tries to search by an email address which doesn't exist. It may not, it depends on how you implement interface methods in your store.

The ```CreateAsync``` method of UserManager calls the ```ValidateAsync``` method of the UserValidator object created by default (or explicitly in the ```Create``` method). If you decompile the validator you can see it calls the following methods of your store prior to actually creating the record:

* ```FindByNameAsync```
* ```FindByEmailAsync```

By default it expects you to return null from your store on these methods where the name or email doesn't exist. To avoid having the validator make assumptions about how you implement your methods you can supply your own class which implements IIdentityValidator as shown below:

	public static ApplicationUserManager Create(
	    IdentityFactoryOptions<ApplicationUserManager> options, 
	    IOwinContext context)
	{
	    var manager = new ApplicationUserManager(new MyUserStore<MyUser>());
	    manager.UserValidator = new MyUserValidator<MyUser, string>();
		...
	}

Where ```MyUserValidator : IIdentityValidator```.


### SignInManager

The sign in manager works with the user manager to validate user logins against the store and provide login information. The default sign in manager is pretty small which makes our change easy, shown below is the default implementation:

	public class ApplicationSignInManager : SignInManager<ApplicationUser, string>
	{
	    public ApplicationSignInManager(ApplicationUserManager userManager, 
	        IAuthenticationManager authenticationManager)
	        : base(userManager, authenticationManager)
	    {
	    }
	
	    public override Task<ClaimsIdentity> CreateUserIdentityAsync(ApplicationUser user)
	    {
	        return user.GenerateUserIdentityAsync((ApplicationUserManager)UserManager);
	    }
	
	    public static ApplicationSignInManager Create(IdentityFactoryOptions<ApplicationSignInManager> options, 
	        IOwinContext context)
	    {
	        return new ApplicationSignInManager(context.GetUserManager<ApplicationUserManager>(), 
	            context.Authentication);
	    }
	}

We can replace this with an almost identical replacement:

	public class ApplicationSignInManager : SignInManager<MyUser, string>
	{
	    public ApplicationSignInManager(ApplicationUserManager userManager, 
	        IAuthenticationManager authenticationManager)
	        : base(userManager, authenticationManager) { }
	
	    public static ApplicationSignInManager Create(
	        IdentityFactoryOptions<ApplicationSignInManager> options, 
	        IOwinContext context)
	    {
	        return new ApplicationSignInManager(context.GetUserManager<ApplicationUserManager>(), 
	            context.Authentication);
	    }
	}

However here I removed the ```CreateUserIdentityAsync(ApplicationUser user)``` method.

I removed the method from the user also. This is because it seems odd to have the user take responsibility for creating itself with the manager. You're welcome to implement it if you wish but it just seemed strange to me.

Removing this breaks things in a couple of places. The most important one is in ```App_Start/Startup.Auth.cs```:

	// Enables the application to validate the security stamp when the user logs in.
	// This is a security feature which is used when you change a password or add an external login to your account.  
	OnValidateIdentity = SecurityStampValidator.OnValidateIdentity<ApplicationUserManager, ApplicationUser>(
	    validateInterval: TimeSpan.FromMinutes(30),
	    regenerateIdentity: (manager, user) => user.GenerateUserIdentityAsync(manager)
	    )

Luckily this method just calls through to the ```CreateIdentityAsync``` method on the UserManager so we can change the above Startup code to:

	OnValidateIdentity = SecurityStampValidator.OnValidateIdentity<ApplicationUserManager, MyUser>(
	        validateInterval: TimeSpan.FromMinutes(30),
	        regenerateIdentity: (m, u) => 
	            m.CreateIdentityAsync(u, DefaultAuthenticationTypes.ApplicationCookie))

### Rounding Up

We have implemented a bare-bones system, the code won't compile until you change all references to ApplicationUser to use your User implementation. Additionally many method calls will fail at run-time for things like 2 Factor and Claims-Based Authentication.

However if you remove enough code from the template controllers you should have a system which allows you to register and login/out. It also validates users based on the ```[Authorize]``` attribute on controllers.

I hope to write a post about implementing things like Password Reset and Roles but I've put this up now to help anyone who is stuck. 

### Conclusion
This tutorial showed how to use ASP.NET Identity 2.0 without Entity Framework. It does not use many of the new features of Identity 2.0 but shows how to create a basic login system with Identity 2.0.

The reliance of Identity 2.0 on EF Code First makes sense when viewed in the context of the [retirement of Database/Model First in EF7][link1]. However it makes things difficult for people not using EF (Code First). There are many open source providers for other databases such as MongoDb available on NuGet and this tutorial hopefully explains a bit about how it all fits together.

In order to extend this simple identity system you'll need a good decompiler to see what's going on under-the-hood in the UserManager class when you run into problems. I can recommend [dotPeek][link2].

I hope this helps you, it aims to be the guide I wish I had when I started writing it. Any problems please leave a comment.

[link0]: http://eliot-jones.com/2014/10/asp-identity-2-0 "First Page"
[link1]: https://www.youtube.com/watch?v=Ie_0k1_9LJ0&list=PL0M0zPgJ3HSftTAAHttA3JQU4vOjXFquF&index=3 "vNext Community Standup (video)"
[link2]: https://www.jetbrains.com/decompiler/ "JetBrains dotPeek decompiler"