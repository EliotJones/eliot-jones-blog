# FakeItEasy Heisenbugs #

**Update:** the problem has been solved in version 2.0.0 of FakeItEasy.

I was recently facing an issue where my FakeItEasy tests were sometimes randomly failing. I'm specifying that it was the FakeItEasy tests because I managed to isolate to the failures to 2 tests which were using:

	var thing = A.Fake<IInterface>();

The FakeItEasy fake would sometimes ignore the specified setup, such as:
	
	A.CallTo(() => thing.Method()).Returns(true);

The fake would return false or null for reference types.

Analysing the stack trace on the failing test it was due to aggregate Exceptions occurring when running the tests in parallel. There is an [issue describing this here][link0].

We were using xUnit as our testing framework which runs tests in parallel by default. Unfortunately both FakeItEasy and Moq aren't threadsafe and this introduces bugs when tests are run in parallel. It seems like other testing frameworks also experience the same problem.

To get around this we [disabled parallel test execution in xUnit][link1]. This means our tests take longer but are reliable.

If you've run into this issue before and found a better fix I'd be intrigued to hear it.

[link0]: https://github.com/FakeItEasy/FakeItEasy/issues/60 "GitHub issue for parallel tests"
[link1]:https://github.com/xunit/xunit/issues/244