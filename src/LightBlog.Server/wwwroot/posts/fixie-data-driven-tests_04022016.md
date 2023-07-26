# Fixie Data Driven tests #

I tend to spend all my coding time nowadays plagued with guilt. I try to be a good developer and refactor the code, after running the tests, after writing the code, after running the tests, after writing the tests. But in the interests of getting any code written at all, I end up skipping between one and all of these steps (generally not the writing code part).

If this sounds like you, I cannot recommend [Fixie][fixie] strongly enough. 

It's a low ceremony test framework that presents the minimum possible barrier to entry for the guilt driven tester/developer.

### Positives ###

Fixie is so nice for a few reasons. Firstly it's self contained. After installing from NuGet, the Visual Studio test runner for Fixie is already set up.

This means you don't have to locate the correct package for your Visual Studio version. If you're lazy like me then you'll appreciate this time-saver. 

To install from the package manager console use:

    Install-Package Fixie

Or search "fixie" on the NuGet package manager.

The next point in its favour is your tests are just code. While MSTest and to some extent NUnit are an alphabetti spaghetti of attributes which challenge you to remember the difference between your ```[TestFixture]``` and ```[TestClass]``` attributes, Fixie requires no attributes to start working.

By default any ```public void``` or ```public async Task``` method is detected as a test as long as the parent class name ends with "Tests". Test set-up and tear-down are handled by constructors and ```IDisposable```.

An example test:

    public class OrderDateTests : IDisposable
    {
        private readonly DateTime frozenDate;

        public OrderDateTests()
        {
            frozenDate = new DateTime(2016, 02, 04);
            SystemTime.Freeze(frozenDate);
        }

        public void OrderDateCannotBeInFuture()
        {
            try
            {
                var order = new OrderDate(frozenDate.AddDays(7));
            }
            catch (OrderDateException)
            {
                // Could run assertion here if desired
            }
        }

        public void OrderDateCanBeCurrentDate()
        {
            var order = new OrderDate(frozenDate);

            // Assert.Equal(frozenDate, order.Date);
        }

        public void Dispose()
        {
            SystemTime.UnFreeze();
        }
    }

This uses the constructor to freeze the SystemTime (class not shown) in order to test the constructor of the class OrderDate (also not shown). OrderDate uses ```SystemTime.Now``` internally.

After each test Fixie will take care of unfreezing the system time based on the logic in ```Dispose()```.

As you can see from the example, the test methods are about as clean as it's possible to get without not having tests. Your test class doesn't even need to reference Fixie, it's a blissful escape from the moral burden of writing tests because it's just writing code.

Another plus point is the massive array of configuration options if the defaults don't suit you. I have only skimmed the surface but there are full details in the [documentation][fixiedocs].

### Undecided Points ###

At the time of writing Fixie doesn't run tests in parallel by default. For some use cases this is positive (e.g. integration tests) but some people may miss the parallel speed boost (not that I've found parallelism to ever deliver a speed boost).

You may also find yourself hunting around for the ```Assert.Equal()``` classes  and methods you're used to from other test frameworks. Fixie doesn't include assertions because the library authors believe that the choice of assertion library is unrelated to the test framework. 

After initially being bewildered by this I now simply install [Shouldly][shouldlydownload], and feel like the exclusion of assertions is a Good Thing (TM); but your mileage may vary.

### Extending It ###

This is where it transpires this entire post is an advertorial. 

Currently Fixie doesn't have support for data driven tests out of the box. It's easy enough to set up and the documentation details it well but it's still a barrier for the lazy tester.

Data driven tests are where one test method runs multiple test cases by passing parameters into that method.

In an entirely artificial example let's say an OrderDate may be created 5 or more days in the future (but not between 1 to 4 days in the future).

Using xUnit style ```InlineData``` attributes we could test this as follows:

    [InlineData(1)]
    [InlineData(2)]    
    [InlineData(3)]
    [InlineData(4)]
    public void OrderDateCannotBeLessThan5DaysInFuture(int daysInFuture)
    {
        try
        {
            var order = new OrderDate(frozenDate.AddDays(daysInFuture));
        }
        catch (OrderDateException)
        {
            // Could run some assertion here if desired
        }
    }

This will run 4 tests to check we cover the range we're expecting to be disallowed.

Maybe we also want to prevent a future date being a Saturday. You may think we could use the same in-line data attribute approach, e.g.:

    [InlineData(new DateTime(2016, 03, 9))]

However C# attributes cannot contain ```DateTime``` or ```decimal``` data. This is due to Common Language Runtime (CLR) [restrictions on how attributes are  stored in the compiled code][stackoverflow]. This means we can't use our ```InlineData``` attribute for tests where the data isn't a primitive CLR type.

Fortunately xUnit provides the ```MemberData``` attribute for just this occasion. This attribute looks for a static method, property or field on a class based on the name of the member. 

For our not-on-Saturdays test this might work as follows:

    // The member used to provide test data
    public static IEnumerable<object[]> SaturdayDates
    {
        get
        {
            return new[]
            {
                new object[] {new DateTime(2016, 03, 5)},
                new object[] {new DateTime(2016, 03, 9)},
                new object[] {new DateTime(2016, 07, 9)}
            };
        }
    }

    // The test
    [MemberData("SaturdayDates")]
    public void OrderDateCannotBeOnSaturdays(DateTime saturdayDate)
    {
        try
        {
            var order = new OrderDate(saturdayDate);
        }
        catch (OrderDateException)
        {
        }
    }

Take my word for it that the Saturday dates in the property are all actually on Saturday.

This example will run 3 tests from the single test method to verify that a set of different Saturdays throws. You'd probably choose better edge cases, far into the future or closer to the SystemTime date, to check against.

I've carefully made a point of saying these are xUnit attributes, but luckily for you, dear reader, some kind soul has created the equivalent attributes in a [handy NuGet package][uglytoad] for Fixie.

To install from the package manager console use:

    Install-Package UglyToad.Fixie.DataDriven

Or searching "fixie" in the NuGet package manager should return it.

Once it's installed you need to define a custom Fixie convention in order to use the attributes. This convention class can live anywhere in your test project. For example:

    using Fixie;
    using UglyToad.Fixie.DataDriven;

    public class TestConfiguration : Convention
    {
        public TestConfiguration()
        {
            Classes.NameEndsWith("Tests");

            Methods.Where(method => method.IsVoid());

            Parameters
                .Add<ProvideTestDataFromInlineData>()
                .Add<ProvideTestDataFromMemberData>();
        }
    }

Then the attributes are available in your test project and will be detected by the test runner. The attributes are in the namespace ```UglyToad.Fixie.DataDriven```. 

Have fun redeeming your immortal programming soul with some tests!

[fixie]: http://fixie.github.io/ "Fixie homepage"
[fixiedocs]: http://fixie.github.io/docs/ "Fixie documentation"
[shouldlydownload]: https://www.nuget.org/packages/Shouldly/ "NuGet Shouldly download"
[uglytoad]: https://www.nuget.org/packages/UglyToad.Fixie.DataDriven/ "NuGet Fixie.DataDriven download"
[stackoverflow]: http://stackoverflow.com/a/507533/1775471 "StackOverflow answer detailing primitive type attribute restrictions"



