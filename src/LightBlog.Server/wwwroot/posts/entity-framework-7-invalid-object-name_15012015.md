# EF7 Table Mapping Exception #

**Note: This blog post relates to a library undergoing development and as such the information is likely to become outdated.**

Even with Database First through the EDMX gone in Entity Framework 7 it's [still possible to work with existing databases][link0].

While trying this out with one of my databases I ran into the following Exception:

	<Message>An error has occurred.</Message>

	<ExceptionMessage>Invalid object name 'SomeClass'.</ExceptionMessage>

	<ExceptionType>System.Data.SqlClient.SqlException</ExceptionType>

	<StackTrace>
	at System.Data.SqlClient.SqlConnection.OnError(SqlException exception, Boolean breakConnection, Action`1 wrapCloseInAction)[...]
	</StackTrace>

The Entity Framework "Invalid object name [class name]" exception means that the matching table for one of your classes hasn't been found.

In this case I'm trying to map the SomeClass to the underlying SQL table ```Map.Test```:

    [Table("Test", Schema="Map")]
    public class SomeClass
    {
        public int Id { get; set; }
    }

The current version of EF7 (7.0.0-rc1-11953) does not have support for mapping using attributes in this way. Instead one must use Fluent configuration in the ```DbContext``` as follows:

	public class MyContext : DbContext
    {
        public DbSet<SomeClass> SomeClasses { get; set; }

        protected override void OnConfiguring(DbContextOptions options)
        {
            options.UseSqlServer(ConnectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<SomeClass>().ForRelational().Table(tableName: "Test", schemaName: "Map");
        }
    }

The mapping is configured fluently in the ```OnModelCreating``` method. For slightly more useful information about setting EF7 up [see this link][link1].

I hope this helps!

[link0]: http://cpratt.co/entity-framework-code-first-with-existing-database/ "Chris Pratt on EF7 Code First"
[link1]: https://github.com/aspnet/EntityFramework/wiki/Using-EF7-in-Traditional-.NET-Applications "Entity Framework 7 Github"