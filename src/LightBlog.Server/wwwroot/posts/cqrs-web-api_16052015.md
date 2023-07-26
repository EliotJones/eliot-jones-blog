# 3-tier CQRS using Web API as a command/query bus #


We recently started using CQRS on a project to provide a new web application to manage a complicated permit process. The CQRS pattern combined with a proper domain model seemed like a good fit for the business logic.

Midway through development of our 2 tier solution (Web <-> Database) we were asked to instead asked to develop a 3 tier solution (Web <-> Api <-> Database) to avoid exposing the database to the web server (no WCF allowed).

Initially the change slowed development and resulted in an explosion in the Lines of Code to functionality ratio. After some thinking we realised it might be possible to use Web API as a command/query bus for our pre-existing CQRS setup, effectively allowing us to "pass-through" the API without needing to write a controller action per command.

This post summarises the ideas behind the working approach we developed. All the code used in this post can be found [on Github][link1] but the code is very obviously simplified; both to avoid revealing implementation specific details such as security and to allow this post to focus on the concepts.

### CQRS ###

If you're not familiar with CQRS [read some of the articles][link2] available on the advantages and disadvantages of the pattern. Basically you separate commands (actions which update data) from queries (which read data).

This post will only deal with implementing the query side of CQRS over Web API. Commands should be simpler.

```Query``` and ```QueryHandler``` interfaces are defined in a separate CQRS class library (ideally Queries and Query handlers live in separate libraries because your web project shouldn't have to reference query handlers).

    public interface IQuery<TReturn>
    {
    }

    public interface IQueryHandler<TQuery, TReturn> where TQuery : IQuery<TReturn>
    {
        Task<TReturn> Send(TQuery query);
    }

### All aboard the query bus ###

![Picture of a bus](https://eliot-jones.com/images/cqrs-api/bus.jpg)

We generally want to use our bus as a mediator so we don't have to specifically tie queries and handlers together. The idealised implementation is:

	TQueryResult result = await queryBus.SendAsync(new Query());

The bus should find and call the correct handler for the query and return the result. In this implementation the Web API controller action replaces the bus.

    public class QueryController : ApiController
    {
        private readonly IServiceLocator serviceLocator;

        public QueryController()
        {
            serviceLocator = new NaiveLocator();
        }

        public async Task<object> Send(QueryRequest query)
        {
            // TODO: Implement the quey bus, find the right handler for the query.
        }
    }

I haven't wired up a dependency injection container in this example so created a very simple service locator (anti-pattern!):

    internal interface IServiceLocator
    {
        object Resolve(Type serviceType);
    }

The implementation of this locator takes the type of ```QueryHandler``` to find and returns that handler. In a real example this could be replaced with a dependency injection container's resolve method:

    internal class NaiveLocator : IServiceLocator
    {
        private readonly Dictionary<Type, object> services; 

        public NaiveLocator()
        {
            this.services = new Dictionary<Type, object>();

            services.Add(typeof(IQueryHandler<GetAllRappers, Rapper[]>), new GetAllRappersHandler(new Data()));
            services.Add(typeof(IQueryHandler<GetRapperByName, Rapper>), new GetRapperByNameHandler(new Data()));
        }

        public object Resolve(Type serviceType)
        {
            return services[serviceType];
        }
    }

### Getting on the bus ###

Because we are sending our queries as HTTP requests to our API the queries have to be serialized somehow and all generic type parameter information is lost.

One approach is to wrap the query as Json in a parent Json object which also contains type information:

    public class QueryRequest
    {
        public string QueryTypeName { get; set; }

        public string QueryData { get; set; }
    }

When the website sends the request it has to generate this object (this method is in ```ApiClient``` in the Api.Client project):

    public async Task<TResult> Send<TQuery, TResult>(TQuery query) where TQuery : IQuery<TResult>
    {
        var request = new QueryRequest
        {
            QueryData = JsonConvert.SerializeObject(query),
            QueryTypeName = typeof(TQuery).AssemblyQualifiedName
        };

        var response = await client.PostAsJsonAsync("api/Query/Send", request);

        return await response.Content.ReadAsAsync<TResult>();
    }

It's important to use the ```AssemblyQualifiedName``` rather than ```FullName``` or just ```Name```.

### The wheels on the bus ###

When our QueryRequest object arrives the API knows nothing about it. The first thing to do is work out what the query is and what we're expected to return. In our API controller we construct an object representing what we know about the request:

	var typeInformation = new QueryRequestTypeInformation(query);

The constructor for ```QueryRequestTypeInformation``` looks like this:

    internal class QueryRequestTypeInformation
    {
        public Type QueryType { get; private set; }

        public Type ResultType { get; private set; }

        public QueryRequestTypeInformation(QueryRequest query)
        {
            this.QueryType = Type.GetType(query.QueryTypeName);
            this.ResultType = this.QueryType.GetInterfaces().Single(i => i.Name.Contains("IQuery")).GetGenericArguments()[0];
        }
    }

This object will now contain information about the query type and the result type.

We can then use this to work out the type of handler we need our Service Locator to locate (in the API controller):

    var queryObject = JsonConvert.DeserializeObject(query.QueryData, typeInformation.QueryType);

    var handlerType = typeof(IQueryHandler<,>).MakeGenericType(typeInformation.QueryType, typeInformation.ResultType);

Jonas Gauffin's [CQS library][link3] provided the inspiration for much of the controller code. 

Given we now have the type of the handler we need, we can request it from our service locator (yes, I'm trying to mention the name of that particular anti-pattern enough times to drive home the point that this isn't the best code, but it works):

	var handler = serviceLocator.Resolve(handlerType);


### A moment of reflection ###

The rest of the code uses reflection heavily, of course there are performance penalties associated with the use of reflection. So far this seems negligible compared to the time for 2 web request/responses (client -> web -> api -> web -> client) and the data access time.

To call the right handler for our query and return an object the API controller then does this:

    var sendMethodInfo = handlerType.GetMethod("Send");

    return await (dynamic)sendMethodInfo.Invoke(handler, new[] { queryObject });

I wasn't aware that dynamics could be awaited in this manner until reading a comment from Stephen Cleary on my [Stack Overflow question on the topic][link4].

The full API controller code then looks like this:

    public async Task<object> Send(QueryRequest query)
    {
        var typeInformation = new QueryRequestTypeInformation(query);

        var queryObject = JsonConvert.DeserializeObject(query.QueryData, typeInformation.QueryType);

        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(typeInformation.QueryType, typeInformation.ResultType);

        var handler = serviceLocator.Resolve(handlerType);

        var sendMethodInfo = handlerType.GetMethod("Send");

        return await (dynamic)sendMethodInfo.Invoke(handler, new[] { queryObject });
    }

### Summary ###

As mentioned in the introduction this is not a complete sample and should not be used in production! For a start there's no security at all between the API and web layer. Additionally there's no error handling anywhere.

This post outlines a possible approach to a generic endpoint for a Web API.

[Full code with working sample][link1].

*bus: https://upload.wikimedia.org/wikipedia/commons/thumb/3/3a/ICCE_Fist_Student_Wallkill_bus.JPG/640px-ICCE_Fist_Student_Wallkill_bus.JPG*

[link1]: https://github.com/EliotJones/MediAPIr
[link2]: http://martinfowler.com/bliki/CQRS.html
[link3]: https://github.com/jgauffin/dotnetcqs/blob/master/src/DotNetCqs.Autofac/ContainerQueryBus.cs
[link4]: http://stackoverflow.com/questions/30176781/implication-of-calling-result-on-awaited-task
[link5]: http://identityserver.github.io/Documentation/docs/overview/bigPicture.html