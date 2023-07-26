#JSON.NET

[JSON.NET][link0] by James Newton-King is *the*  library for working with JSON in .NET. The following is a small guide for using JSON.NET. It is in no way a substitute for the [full documentation][link1].

To follow along obtain the JSON.NET package using NuGet and the **Newtonsoft.Json** dll will be added to your project's references. Alternatively download from the official website and add the reference manually.

### Serialize An Object And De-serialize
*Serialization is the process of translating data structures or object state into a format that can be stored* - Wikipedia.

We will start our investigation with a very simple C# object and continue from there. As always, we are using the Dog class:

    public class Dog
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Breed { get; set; }
        public DateTime Birthday { get; set; }

        public int CalculateAgeInDays()
        {
            return (DateTime.Now - Birthday).Days;
        }
    }
This is very simple class but might mirror something you need to serialize.

To start, let's create a dog and convert it to a string of Json (which I'll stop capitalising because it's a pain to type).

	Dog dog = new Dog
	{
		Id = 4,
		Breed = "Labradoodle",
		Name = "Baron Von Lassie",
		Birthday = Convert.ToDateTime("2013-01-07"),
	};
	string json = JsonConvert.SerializeObject(dog);

The string that we obtain is 'minified':

	{"Id":4,"Name":"Baron Von Lassie","Breed":"Labradoodle","Birthday":"2013-01-07T00:00:00"}

As you can see all extraneous whitespace and extra line-breaks have been removed. This is ideal for data transfer objects such as a response from a Web Service, however if we're trying to present our data in a human readable way it's nicer to set ```Formatting.Indented``` like so:

	string json = JsonConvert.SerializeObject(dog, Formatting.Indented);

This gives the neater response:

	{
	  "Id": 4,
	  "Name": "Baron Von Lassie",
	  "Breed": "Labradoodle",
	  "Birthday": "2013-01-07T00:00:00"
	}

This reveals that more complex datatypes like DateTime are stored as strings in Json. Json.NET uses the [ISO standard 8601][link2] for dates, you can read more about why on [Scott Hanselman's blog][link3].

We can get our dog back again using:

	Dog lassie = JsonConvert.DeserializeObject<Dog>(json);

### Serialize A More Complex Object And Serializing To A File
We can now create and retrieve a Json representation of our dog, but how about persisting it somewhere on our hard-drive? 

First let's add a list of collars to our Dog object, this will show an object with an object array in Json:

	public class Collar
    {
        public int Id { get; set; }
        public string Color { get; set; }
        public bool HasTag { get; set; }
    }

The collars list property is added to the dog class and the dog now has 2 collars using nice collection initialization:

	List<Collar> collars = new List<Collar>
    {
        new Collar { Id = 1, Color = "Red", HasTag = true },
        new Collar { Id = 2, Color = "Pink", HasTag = false }
    };
	dog.Collars = collars;

JsonConvert from the previous example provides a simple way to write and read to/from a string. This provides a wrapper to ```JsonSerializer```.

The simplest way to serialize to a file is shown:

	string folder = "C:\\Test\\";
	using (StreamWriter file = File.CreateText(folder + "dog.json"))
	{
	    JsonSerializer serializer = new JsonSerializer();
	
	    serializer.Formatting = Formatting.Indented;
	
	    serializer.Serialize(file, dog);
	} 

The text file then contains our dog object, preserved forever(!):

	{
	  "Id": 4,
	  "Name": "Baron Von Lassie",
	  "Breed": "Labradoodle",
	  "Collars": [
	    {
	      "Id": 1,
	      "Color": "Red",
	      "HasTag": true
	    },
	    {
	      "Id": 2,
	      "Color": "Pink",
	      "HasTag": false
	    }
	  ],
	  "Birthday": "2013-01-07T00:00:00"
	}

The dog can be retrieved from the file as follows:

	using (StreamReader file = File.OpenText(folder + "dog.json"))
	{
	    JsonSerializer serializer = new JsonSerializer();
	    Dog lassie = (Dog)serializer.Deserialize(file, typeof(Dog));
	}
Serialization and deserialization can be thought of as hydration/dehydration, dehydrate your dog to store it and stop it rotting, then rehydrate it to play in the park with.

### Json Settings
We've already seen one serialization setting, ```Formatting.Indented``` in the examples above. There are many more serialization options for which [the documentation][link4] should be your first port of call.

For the simple Json serializer (JsonConvert) settings are stored in the JsonSerializerSettings object:

	JsonSerializerSettings settings = new JsonSerializerSettings
    {
        ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
        Culture = CultureInfo.CurrentCulture,
        Formatting = Formatting.Indented,
        MissingMemberHandling = MissingMemberHandling.Error
    };
    string json = JsonConvert.SerializeObject(dog, settings);
Just out of interest I serialized the JsonSerializerSettings object from above:

	{
	  "ReferenceLoopHandling": 0,
	  "MissingMemberHandling": 1,
	  "ObjectCreationHandling": 0,
	  "NullValueHandling": 0,
	  "DefaultValueHandling": 0,
	  "Converters": [],
	  "PreserveReferencesHandling": 0,
	  "TypeNameHandling": 0,
	  "TypeNameAssemblyFormat": 0,
	  "ConstructorHandling": 1,
	  "ContractResolver": null,
	  "ReferenceResolver": null,
	  "TraceWriter": null,
	  "Binder": null,
	  "Error": null,
	  "Context": {
	    "Context": null,
	    "State": 0
	  },
	  "DateFormatString": "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK",
	  "MaxDepth": null,
	  "Formatting": 1,
	  "DateFormatHandling": 0,
	  "DateTimeZoneHandling": 3,
	  "DateParseHandling": 1,
	  "FloatFormatHandling": 0,
	  "FloatParseHandling": 0,
	  "StringEscapeHandling": 0,
	  "Culture": "en-GB",
	  "CheckAdditionalContent": false
	}
Here you can see something else to note, by default Enums are serialized using their integer value, rather than string. For instance Formatting has the value 1 rather than "Indented".

By adding a Converter from ```Newtonsoft.Json.Converters``` the string value of an enum can be serialized:

	serializer.Converters.Add(new StringEnumConverter());

For the JsonSerializer the settings are properties of the object itself. JsonSerializerSettings can be mapped to a JsonSerializer on initialization ```JsonSerializer serializer = JsonSerializer.Create(settings);```.

One important setting for deserialization is ```MissingMemberHandling```, this controls what happens when an additional property is present in the Json that isn't a property on the target object. If the value is set to ```MissingMemberHandling.Error``` a JsonSerializationException is thrown when an extra value in the Json is encountered.

The name for this is slightly confusing since actual missing members (e.g. a property is present on the object but not in the Json) aren't dealt with, the default value of the property (0 for int, false for boolean, etc.) is used.

### Json Attributes

A higher level of control over serialization can be achieved by adding attributes to classes. For example, if you only want some properties to be serialized you can add the **JsonObject** attribute to the class then **JsonProperty** to each property to include.

For more on this see [this page of the documentation][link5].

[link0]: http://james.newtonking.com/json "JSON.NET official page"
[link1]: http://james.newtonking.com/json/help/index.html "Official JSON.Net Documentation"
[link2]: http://en.wikipedia.org/wiki/ISO_8601 "ISO 8601 on Wikipedia"
[link3]: http://www.hanselman.com/blog/OnTheNightmareThatIsJSONDatesPlusJSONNETAndASPNETWebAPI.aspx "Scott Hanselman on JSON dates"
[link4]: http://james.newtonking.com/json/help/index.html?topic=html/SerializationSettings.htm "Serialization Settings"
[link5]: http://james.newtonking.com/json/help/index.html?topic=html/SerializationAttributes.htm "Serialization Attributes"