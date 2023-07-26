# ASP.NET Identity 2.0 Tutorial - Password Reset and Roles#

Given how much you all enjoyed the previous tutorial, i.e. not at all, I thought I'd write a follow up post. As promised this blog post extends the basic system we created in the [main tutorial][link0] to add roles and a password reset function.

I won't show how to setup the emails for password reset because I'm too lazy to fill in the form for a free SendGrid account, however plugging emails in should be the easy bit.

### Password Reset

The default template provides the controller actions and views for a full password reset function. The user can enter an email address at ```/Account/ForgotPassword```. When posted the action below is called:

	public async Task<ActionResult> ForgotPassword(ForgotPasswordViewModel model)
	{
	    if (ModelState.IsValid)
	    {
	        var user = await UserManager.FindByNameAsync(model.Email);
	        if (user == null || !(await UserManager.IsEmailConfirmedAsync(user.Id.ToString())))
	        {
	            // Don't reveal that the user does not exist or is not confirmed
	            return View("ForgotPasswordConfirmation");
	        }
	        string code = await UserManager.GeneratePasswordResetTokenAsync(user.Id.ToString());
	        var callbackUrl = Url.Action("ResetPassword", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);		
	        // await UserManager.SendEmailAsync(user.Id, "Reset Password", "Please reset your password by clicking <a href=\"" + callbackUrl + "\">here</a>");
	        return RedirectToAction("ForgotPasswordConfirmation", "Account");
	    }
	    return View(model);
	}

I have commented out the email sending part for the reasons mentioned above.

All we need to do is supply our ```UserManager``` with a token provider. From the code for the default UserManager:

	/// <summary>
	/// Used for generating reset password and confirmation tokens
	/// </summary>
	public IUserTokenProvider<TUser, TKey> UserTokenProvider { get; set; }

As usual Microsoft have made everything easy by giving us a nice interface to implement. This also means you can adapt your password reset to function however you like. For instance you could add an expiry date to the reset token or have a token which is a combination of a Guid and number or two Guids.

For my token provider I'm simply going to generate a new Guid to act as the reset token. This maps onto the nullable reset token field on my user table, I've hidden some stuff here because I don't want you stealing the super secure password hashes ("password" and "password1"):

<img src="https://eliot-jones.com/images/aspidentity2/userTable.png" alt="Screenshot of user table schema." />

Now we create a class which implements the required 4 methods for a token provider:

	public class MyUserTokenProvider<TUser> : IUserTokenProvider<MyUser, string>  where TUser : class, IUser
	{
	    public Task<string> GenerateAsync(string purpose, UserManager<MyUser, string> manager, MyUser user)
	    {
	        Guid resetToken = Guid.NewGuid();
	        user.PasswordResetToken = resetToken;
	        manager.UpdateAsync(user);
	        return Task.FromResult<string>(resetToken.ToString());
	    }
	
	    public Task<bool> IsValidProviderForUserAsync(UserManager<MyUser, string> manager, MyUser user)
	    {
	        if (manager == null) throw new ArgumentNullException();
	        else {
	            return Task.FromResult<bool>(manager.SupportsUserPassword);
	        }
	    }
	
	    public Task NotifyAsync(string token, UserManager<MyUser, string> manager, MyUser user)
	    {
	        return Task.FromResult<int>(0);
	    }
	
	    public Task<bool> ValidateAsync(string purpose, string token, UserManager<MyUser, string> manager, MyUser user)
	    {
	        return Task.FromResult<bool>(user.PasswordResetToken.ToString() == token);
	    }
	}

+ The 0 value returned from ```NotifyAsync``` mirrors the way it's done in the ```TotpSecurityStampBasedTokenProvider``` included in the Identity library. 
+ I don't call through to the store in the ```ValidateAsync``` method because the user argument will have been retrieved by the manager just before this method call so there's no point duplicating data access.
+ ```IsValidProviderForUserAsync``` should probably be implemented differently however this does what I want so why change? :) 
 
The last thing we need to do is tell our UserManager about its swanky new token provider. You can either throw it in the overloaded constructor or ```Create``` method. Here I've put it in the constructor:
	
	public class ApplicationUserManager : UserManager<MyUser>
	{
	    public ApplicationUserManager(IUserStore<MyUser> store)
	        : base(store)
	    {
	        this.UserValidator = new MyUserValidator<MyUser, string>();
	        this.UserTokenProvider = new MyUserTokenProvider<MyUser>();
	    }
	}

Now if you go to the link contained in the ```callbackUrl``` variable in the controller action you can reset a user's password.

#### Summary
1. Give your persistence layer some kind of a password reset token.
2. Create a class which implements ```IUserTokenProvider```.
3. Pass that class to your UserManager class as the manager's ```UserTokenProvider```.
4. ?
5. Profit

### Roles
The UserManager class has a set of boolean properties of the form:
	
	/// <summary>
	/// Returns true if the store is an IUserPasswordStore
	/// </summary>
	public virtual bool SupportsUserPassword
	{
	  get
	  {
	    return Store is IUserPasswordStore<TUser, TKey>;
	  }
	}

The booleans check which interfaces your ```UserStore``` implements. These booleans are used to throw descriptive exceptions where your store is lacking the methods the manager depends on. The full list is:

+ IUserTwoFactorStore
+ IUserPasswordStore
+ IUserSecurityStampStore
+ **IUserRoleStore**
+ IUserLoginStore
+ IUserEmailStore
+ IUserPhoneNumberStore
+ IUserClaimStore
+ IUserLockoutStore
+ IQueryableUserStore

The IUserStore has to be used by default so isn't included. Whenever we want to use a piece of the manager's functionality generally all we need to do is implement the missing interface on our store.

This extensibility comes in handy for the role store. With roles defined we can do things like this on our controllers:

	[Authorize(Roles="Elevated Browser")]
	public ActionResult Contact()
	{
	    return View();
	}

This makes limiting access to certain actions very easy. Additionally because everything uses interfaces with generic parameters no assumptions are made about our persistence layer.

Let's say users gain points through some mechanism, when they get enough points they unlock certain privileges which can be represented by roles. Because we aren't tied to the Code First models for our store we can have this sort of model for our roles:

<img src="https://eliot-jones.com/images/aspidentity2/roleStore.png" alt="Roles table schema featuring Role, UserRole and Users table." />

A Role requires a certain number of points and a user has points (which should probably be in a UserPoint table). This is not a brilliant way to implement this model but serves as a warning to future generations (example).

It would make sense to have a composite primary key of RoleId and UserId in the UserRole table however I've opted for a single column primary key in case UserRole evolves into some kind of business entity.

Again the fact I'm using Database-First Entity Framework shouldn't matter here because all that you need to worry about is how to implement your own version of the ```IUserRoleStore``` in your store.

Here's mine:

	#region RoleStore
	public Task AddToRoleAsync(MyUser user, string roleName)
	{
	    return Task.Factory.StartNew(() => ChangeRoleMembership(user, roleName, ChangeType.Add));
	}
	
	public Task<IList<string>> GetRolesAsync(MyUser user)
	{
	    using (var db = new EfDbContext())
	    {
	        User userDb = db.Users.Find(user.Id);
	
	        IEnumerable<int> userRoleIds = userDb.UserRoles.Select(ur => ur.RoleId);
	
	        IEnumerable<string> roleNames = db.Roles.Where(r => userRoleIds.Contains(r.Id)).Select(r => r.Name);
	
	        return Task.FromResult<IList<string>>(roleNames.ToList());
	    }
	}
	
	public Task<bool> IsInRoleAsync(MyUser user, string roleName)
	{
	    bool isInRole = false;
	    using (var db = new EfDbContext())
	    {
	        Role roleDb = db.Roles.FirstOrDefault(r => string.Compare(r.Name, roleName) == 0);
	        User userDb = db.Users.Find(user.Id);
	        
	        if (roleDb != null && userDb != null)
	        {
	            UserRole userRole = userDb.UserRoles.FirstOrDefault(ur => ur.RoleId == roleDb.Id);
	            isInRole = userRole != null;
	        }
	    }
	
	    return Task.FromResult<bool>(isInRole);
	}
	
	public Task RemoveFromRoleAsync(MyUser user, string roleName)
	{
	    return Task.Factory.StartNew(() => ChangeRoleMembership(user, roleName, ChangeType.Remove));
	}
	#endregion

The ```ChangeRoleMembership``` method and ```ChangeType``` enum are just things I've implemented and can be swapped out for your own methods.

Without any further changes you should be able to use the roles in your controller attributes.

#### Summary
1. Create some Roles feature in your persistence layer.
2. Add the ```IUserRoleStore``` interface methods to your user store.

### On A Roll(e)

<img src="https://eliot-jones.com/images/aspidentity2/breadrolls.jpg" alt="Bread rolls." />

Hopefully this tutorial series shows how easy it is to make the Identity 2.0 system do what you want. If nothing else you should now have the information necessary to find out what the UserManager expects and provide classes that comply with its demands.

*Roll Image from: http://nl.wikipedia.org/wiki/Bestand:Kaiserbroodjes1151.JPG. Yes I really did scroll through the roll category on Wikimedia... because I care.*

[link0]: http://eliot-jones.com/2014/10/asp-identity-2-0 "ASP NET Identity 2.0 Tutorial"