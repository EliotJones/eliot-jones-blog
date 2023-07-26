# The Curious Case of the Null StringBuilder #

Today was spent tracking down a very weird bug. In production we were seeing an important part of our document reading fail. We kept getting ```NullReferenceException```s when calling ```AppendLine``` on a non-null StringBuilder. It didn't prevent us reading the document however the result would be significantly different to the same document on a local instance of our program.

It only started occurring after the production server had been running for a few days which meant we couldn't debug it locally. We were running .NET 4.5.2.

Luckily we had lots of logging to track down the issue. The problem was in a class like this:

    public class AlgorithmLogicLogger
    {
        private readonly StringBuilder stringBuilder;

        public AlgorithmLogicLogger()
        {
            stringBuilder = new StringBuilder();
        }

        public void Append(string s)
        {
            var message = BuildStringDetails(s);

            stringBuilder.AppendLine(message);
        }

        private static string BuildStringDetails(string s)
        {
            return $"{DateTime.UtcNow}: {s}";
        }
    }

This was a class which was originally intended to provide detailed logging for a complicated algorithm.

The call to ```StringBuilder.AppendLine()``` inside ```Append``` was throwing a ```NullReferenceException```.

After ensuring no weird reflection was taking place and using Ildasm to inspect the compiled code we were sure it wasn't possible for ```stringBuilder``` to be null. It was always instantiated in the constructor and never changed elsewhere.

The next working theory was that a multi-threading issue was somehow calling ```Append``` prior to the field being set. This was also discounted both because it wouldn't have been possible and also because the code in question was not called from multiple threads.

After ensuring that it wasn't the case that the garbage collector wasn't somehow incorrectly collecting the string builder (because it was only written to, never read, the reading hadn't been implemented yet) we were beginning to run out of ideas.

After combing the production logs from the past 3 days we finally located the issue.

The logger was being created as a singleton:

    public class LoggerSingleton
    {
        public AlgorithmLogicLogger Logger { get; } = new AlgorithmLogicLogger();
    }

Since the StringBuilder was never cleared it would eventually grow larger than the ```MaxCapacity```. When this happened an ```OutOfMemoryException``` was thrown and caught further up the stack. Somehow this resulted in a corrupted state which meant further calls to the singleton instance of the StringBuilder such as ```Append``` and ```AppendLine``` resulted in a ```NullReferenceException``` being thrown internally.

Fixing this was fairly simple, just removing the singleton since it was no longer required, but it was odd to see the non-null object throw a ```NullReferenceException``` rather than further ```OutOfMemoryException```s which is what we observed when the code was replicated locally.