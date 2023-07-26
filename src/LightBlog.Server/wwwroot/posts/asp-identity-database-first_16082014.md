#ASP.NET Identity 1.0 - Database First

*Note: This tutorial applies to Identity 1.0 which is the previous version of ASP.NET Identity. I have a newer post on the same subject* [here][linkEdit]

If you've read my previous posts you probably know I'm working on some EF Database first stuff, this seems to be the least popular way to use EF but to me seems like a common business use-case for it.

The MVC template in Visual Studio comes with the new Asp.NET Identity system built in, but it's designed to be used with Code First. This leads to people asking [how to use Identity with Database First?][link0] The suggested soultion is to use the table structure provided when you use Code First and port it to your pre-existing database.

This seemed like an unpleasant approach to me (and a lot of work), especially because I think the database layout for those tables is kinda sucky. This is when it occurred to me that we're basically trying to implement Identity without using what the documentation thinks of as Entity Framework.

When you Google how to do that, you get [this great post][link1] from Mark Johnson. You can follow his approach and just substitute his use of Dapper for use of Entity Framework. for instance:

    public Task CreateAsync(User user)
    {
        if (user == null)
        {
            throw new ArgumentNullException("user was null");
        }

        return Task.Factory.StartNew(() =>
            {
                db.Users.Add(user);
                db.SaveChanges();
            });
    }

	public Task DeleteAsync(User user)
    {
        User checkUser = db.Users.Find(user.Id);

        if (user == null || checkUser == null)
        {
            throw new ArgumentNullException("user was null");
        }

        return Task.Factory.StartNew(() =>
        {
            db.Users.Remove(user);
            db.SaveChanges();
        });
    }

Where ```db``` is your DbContext. Hopefully this helps you as much as it helped me, it's a far nicer solution than rolling your own Auth system.

[link0]: http://stackoverflow.com/questions/20668328/using-asp-net-identity-database-first-approch "StackOverflow Question"
[link1]: http://blog.markjohnson.io/exorcising-entity-framework-from-asp-net-identity/
[linkEdit]: http://eliot-jones.com/2014/10/asp-identity-2-0 "New Tutorial"