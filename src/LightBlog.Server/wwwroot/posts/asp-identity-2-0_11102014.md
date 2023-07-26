# ASP.NET Identity 2.0 Tutorial - Entity Framework Free#

So in [this post][link0] I detailed how to use Asp Identity with Entity Framework Database First. However when I started a new MVC project and tried to follow the steps nothing worked. [ASP Identity 2.0][link1] has ruined everything by introducing many breaking changes. 

This post will guide you on how to setup Identity 2.0 without Entity Framework. The original post was a lot longer but I've tried to cut down a lot of the rambling, for a great step-by-step guide to Identity 2.0 see this [excellent CodeProject post][link2] by John Atten.


### Create a New Project

Let's create a new MVC project using Individual User Accounts as shown below to see what the template gives us:

<img src="https://eliot-jones.com/images/aspidentity2/newProjectScreen.png" alt="The default new project screen." />

This gives us a familiar MVC project. At the root level is the partial class ```Startup.cs```:

	public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }

This makes a call to the other part of this class  ```Startup.Auth.cs``` defined in the App_Start folder. 
This does a lot of things including acting as an IoC container for our ```ApplicationDbContext```
, ```ApplicationUserManager``` and ```ApplicationSignInManager``` using Owin to create an instance for each web request. Additionally it sets up two-factor authentication for SMS verification of log-ins which is a new feature in Identity 2.0.

To remove the dependence on Entity Framework we need to implement everything without the Microsoft.AspNet.Identity.EntityFramework library.

### A Case of Mistaken Identity

The ```ApplicationUserManager``` and ```ApplicationSignInManager``` mentioned are in the file ```IdentityConfig.cs```. This file contains the following 4 classes:

+ EmailService
+ SmsService
+ ApplicationUserManager
+ ApplicationSignInManager

For now we can ignore the EmailService and SmsService. This is where you'd integrate something like [SendGrid][link3] or your own SMTP server to send emails.

### Back to Basics
The layout of classes and what relies on what is actually fairly simple, it just takes a bit of thinking about. This image outlines what we're talking about:

<img src="https://eliot-jones.com/images/aspidentity2/basicDiagram.png" style="border:1px solid #AAA" alt="Diagram outlining 4 main classes." />


We have 4 main classes to worry about:

1. **User** - it's up to us to implement this using the IUser interface. The EF version uses the IdentityUser class but we can't use this.
2. **UserStore** - the user store handles saving and retrieving data from our storage medium. This could be anything you can think of. In this tutorial I'm using EF Database First but it won't affect the EF-free nature of this tutorial.
3. **UserManager** - this is implemented for us by the Microsoft.AspNet.Identity.Core library, it's up to us to provide it with a suitable user and user store implementation.
4. **SignInManager** - this is implemented for us by the Identity.Owin library, we must provide it a suitable user manager and user implementation. 

### User
So Identity 2.0 deals with a user implementation. In the new MVC project template it uses the IdentityUser class. This is in the ```IdentityModels.cs``` file. This file contains 2 classes:

+ ApplicationUser
+ ApplicationDbContext

Any hope we have of easily separating our project from a dependency on EntityFramework is dashed here because IdentityUser is an EF specific class:

	public class ApplicationUser : IdentityUser
    {
        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            var userIdentity = 
                await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            return userIdentity;
        }
    }

By moving everything back onto Core we only have to implement an IUser&lt;TKey&gt; class where TKey is the type of the primary key (e.g. int, Guid, etc).

I implemented the class ```MyUser``` as follows:

	namespace TutorialIdentity.Identity
	{
	    using Microsoft.AspNet.Identity;
	    using System;
	    public class MyUser : IUser<int>, IUser
	    {
	        public int Id { get; set; }
	        string IUser<string>.Id{ get { return Id.ToString(); } }
	
	        public string UserName { get; set; }
	        public string Email { get; set; }
	
	        public string PasswordHash { get; set; }
	        public string SecurityStamp { get; set; }
	        public Guid? PasswordResetToken { get; set; }
	    }
	}

This class is then mapped onto an underlying class in the EF Db First model but could be stored in anything.

I use both the IUser&lt;int&gt; and IUser (equivalent to IUser&lt;string&gt;) interface in order to make life easier when integrating with the existing classes and interfaces.

### UserStore
The user store is required to implement several interfaces. Firstly it must implement the IUserStore<TUser, TKey> interface. From the code for the UserManager class:

	/// <summary>
    /// Persistence abstraction that the UserManager operates against
    /// 
    /// </summary>
    protected internal IUserStore<TUser, TKey> Store { get; set; }

The UserManager takes the store in its constructor.

I created a class ```MyUserStore``` as follows:

	public class MyUserStore<TUser> : IUserStore<MyUser> where TUser : IUser
    {
		...
	}

#### IUserStore
In ```MyUserStore``` this interface is implemented as follows:

	public Task CreateAsync(MyUser user)
	{
	    User userDb = ToUser(user);
	    return Task.Factory.StartNew(() =>
	    {
	        SaveUser(userDb);
	    });
	}
	
	public Task DeleteAsync(MyUser user)
	{
	    throw new System.NotImplementedException();
	}
	
	public Task<MyUser> FindByIdAsync(string userId)
	{
	    return Task.Factory.StartNew<MyUser>(() => FindUser(userId, FindKeyType.Id));
	}
	
	public Task<MyUser> FindByNameAsync(string userName)
	{
	    return Task.Factory.StartNew<MyUser>(() => FindUser(userName, FindKeyType.Name));
	}
	
	public Task UpdateAsync(MyUser user)
	{
	    return Task.Factory.StartNew(() =>
	    {
	        using (var db = new EfDbContext())
	        {
	            User userDbOrig = db.Users.Find(user.Id);
	            userDbOrig.UserName = user.UserName;
	            userDbOrig.PasswordHash = user.PasswordHash;
	            userDbOrig.Email = user.Email;
	            db.Entry(userDbOrig).State = EntityState.Modified;
	            db.SaveChanges();
	        }
	    });
	}

I haven't finished writing UpdateAsync and DeleteAsync methods since **this is just a throwaway tutorial store**. These methods call other methods which use the EF DbContext. However they could just as easily call a webservice or MongoDb or a text file storage mechanism (which would be really weird but I give the example to show that the world is your oyster).

In its current state our UserStore will be accepted by the UserManager at compile time however we can do nothing useful with it at runtime. To get to the point where logins are working we need some more interfaces on our store.

The full list to get to the point where logging in / registering works is:

+ IUserPasswordStore&lt;MyUser&gt;
+ IUserEmailStore&lt;MyUser&gt;
+ IUserLockoutStore<MyUser, string>
+ IUserTwoFactorStore<MyUser, string>
+ IUserSecurityStampStore&lt;MyUser&gt; 

Note that I just chucked in TKey of string where it was required. When you're writing your store you should take time to make sure your TKey is being passed around properly.  All interfaces in the Identity library have ```IInterface<TUser> : IInterface<TUser, string>``` and ```IInterface<TUser, TKey>``` versions.

#### IUserPasswordStore

This requires you to implement the following:

	#region PasswordStore
	public Task<string> GetPasswordHashAsync(MyUser user)
	{
	    return Task.Factory.StartNew<string>(() =>
	    {
	        using (var db = new EfDbContext())
	        {
	            return FindUser(user.Id.ToString(), FindKeyType.Id).PasswordHash;
	        }
	    });
	}
	
	public Task<bool> HasPasswordAsync(MyUser user)
	{
	    return Task.Factory.StartNew<bool>(() =>
	    {
	        using (var db = new EfDbContext())
	        {
	            return !string.IsNullOrEmpty(db.Users.Find(user.Id).PasswordHash);
	        }
	    });
	}
	
	public Task SetPasswordHashAsync(MyUser user, string passwordHash)
	{
	    return Task.Factory.StartNew(() =>
	    {
	        user.PasswordHash = passwordHash;
	    });
	}
	#endregion

Again these are awful store methods just written to get something working. **Don't Use These!**

### IUserEmailStore

Implementing the following methods:

	#region EmailStore
	public Task<MyUser> FindByEmailAsync(string email)
	{
	    return Task<MyUser>.Factory.StartNew(() =>
	    {
	        using (var db = new EfDbContext())
	        {
	            User userDb = db.Users.First(u => u.Email == email);
	            return ToMyUser(userDb);
	        }
	    });
	}
	
	public Task<string> GetEmailAsync(MyUser user)
	{
	    return Task<string>.Factory.StartNew(() => return user.Email);
	}
	
	public Task<bool> GetEmailConfirmedAsync(MyUser user)
	{
	    return Task<bool>.Factory.StartNew(() => return true);
	}
	
	public Task SetEmailAsync(MyUser user, string email)
	{
	    return Task.Factory.StartNew(() => user.Email = email);
	}
	
	public Task SetEmailConfirmedAsync(MyUser user, bool confirmed)
	{
	    throw new NotImplementedException();
	}
	#endregion

This doesn't implement email confirmation properly but this is where you'd do so if your site is using it.

I won't show full implementations for all the interfaces here, because the way they're implemented is rubbish and you can get Visual Studio to tell you what you should be implementing (anyone could write better methods than these!).

The full code for my (**really, really bad**) user store [is here][link4]. As you can see most of it is not implemented yet since a lot of these methods are not used if you remove 2 factor / claims-based auth.

The following 2 interfaces:

	IUserLockoutStore<MyUser, string>,
	IUserTwoFactorStore<MyUser, string>

Must be implemented even if you're not using lockouts (you should) or 2 factor auth. You can just use the method returning ```bool``` to tell the user/sign-in manager they're not enabled, like so:

	public Task<bool> GetTwoFactorEnabledAsync(MyUser user)
	{
	    return Task.Factory.StartNew<bool>(() => false);
	}

### Next Post

This post couldn't grow much longer and still be readable so the next two classes are discussed in the [next post][link5]. Once you've implemented all the interfaces on your store to your satisfaction [continue reading][link5].

[link0]: http://eliot-jones.com/2014/08/asp-identity-database-first "Blog post on my site"
[link1]: http://blogs.msdn.com/b/webdev/archive/2014/03/20/test-announcing-rtm-of-asp-net-identity-2-0-0.aspx "Identity 2.0 announcement on MSDN"
[link2]: http://www.codeproject.com/Articles/762428/ASP-NET-MVC-and-Identity-Understanding-the-Basics "Guide to Identity 2.0"
[link3]: http://sendgrid.com/ "SendGrid email service"
[link4]: http://eliot-jones.com/Code/asp-identity/MyUserStore.cs "Code file on my site"
[link5]: http://eliot-jones.com/2014/10/asp-identity-2-0-p2 "Next Page"