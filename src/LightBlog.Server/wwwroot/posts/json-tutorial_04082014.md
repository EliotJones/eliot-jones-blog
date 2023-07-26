# What Is JSON?
This is intended as a simple guide to JSON.

The more programming you do the more you hear about the data format "JSON".

However I've never actually used it until the changes to ASP.NET in vNext encouraged me to use it, basically the old XML format of the Web.Config is out and JSON is in (though you can swap back if I recall correctly).

### What's In A Name?
JSON stands for **"JavaScript Object Notation"**.

### JavaScript Objects
The origin for this name is clear when you consider JavaScript objects. JavaScript objects are effectively dictionaries of key value pairs.

For those of you not familiar with a dictionary, the concept of JavaScript objects is as follows. 

Let's describe your house:

	var yourHouse = {
		number : 25,
		numberOfWindows : 20,
		name : "Honeysuckle Cottage",
		dog : new Dog(),
		openDoor : function() {
			alert("Hello!");
			}
	};

Now this isn't [the best way to express][link0] a JavaScript Object with methods (or at all for that matter) but it expresses that JS objects are just collections of names for things and their values.

If I ask, what's the name of your house ```yourHouse["name"]``` or ```yourHouse.name``` you will give me the string value ```"HoneySuckle Cottage"```. If I ask your house number ```yourHouse["number"]``` you would give me the integer response 25.

If I open the door to your house ```yourHouse.openDoor()``` you will say "Hello!", or more likely, chase me away with a knife.

Similarly if I ask to meet your dog ```yourHouse["dog"]``` you wouldn't give me a string response, rather you'd show me the dog object.

### Back To JSON
JSON [originated from JavaScript][link1] objects but has no dependence on JavaScript anymore.

Rather it's a format to express/store/exchange data. 

For example the NoSql database [MongoDb][link2] uses binary JSON (BSON) to store its data.

### JSON Syntax Advantages
JSON syntax has similarities with other syntaxes used to store data in a way that both a computer and a person can read, chiefly these are [YAML][link3] and [XML][link4]. 

One advantage is that it's lighter weight (expresses the same information in fewer characters) than XML, so is increasingly used for Data Transfer Objects from Web Services. The reason for this is if I ask your library web service for all 10, 000 books in the library you want to save every possible byte of your bandwidth.

The other major advantage (or perhaps the main one) is compatibility with JavaScript ("the assembly language of the web"). JSON can be converted to a JavaScript object using ```eval()```, **don't do this, due to security**, or with a built in JSON parser.

Obviously there are disadvantages as well, have a quick Google to get a more balanced view.

### Our House, In The Middle Of Our JSON
Enough talk, time to see some JSON:

	{
	  "number" : "25",
	  "numberOfWindows" : 20,
	  "name" : "HoneySuckle Cottage"
	}

That's the object representing your house from earlier in JSON. The first thing to notice is your dog is gone, we'll get to that in a moment.

Also concerning, perhaps more concerning depending on how much you like dogs, is that we can no longer open your door. This is because we cannot store functions in JSON, not even JavaScript functions, which are just JS objects. If you can get over the loss of your door, let's get back to your missing dog.

I didn't include your dog because I wanted to show the simplest JSON syntax first, objects can be nested within other objects like so:

	{
		"number" : "25",
		"numberOfWindows" : 20,
		"name" : "HoneySuckle Cottage",
		"dog" : {
			"age" : 5,
			"name" : "Baron Von Lassie"
		}
	}
In this way complex object containers can be built and stored in an easily readable way.

### Syntax Overview
Let's define this a little more formally.

##### Object
Any object in JSON starts and ends with curly braces '{ }'

An object contains "key" : value pairs where the key is a string identifier for the property of the object.

##### Value
JSON supports the following Data Types for values:

- Number (int, float, double etc all in the same DataType)
- String ("see [me][link5] for more string and syntax rules")
- Boolean (true or false unquoted)
- null
- Array
- Object

##### Array
An array can contain objects:

	"LightSources": [
	    {
	      "Id": 1,
	      "Name": "candle"
	    },
	    {
	      "Id": 2,
	      "Name": "torch"
	    }
	  ]

Or other DataTypes, e.g. numbers:

	"Times": [
	    10.2345,
	    15.0
	  ]

### JSON.NET
"Hmm", you say, "these JSON objects sure look like a good fit for C# objects!".

Well I'm glad you mentioned it, because in the next post we will look at some JSON.NET

[link0]: http://stackoverflow.com/questions/504803/how-do-you-create-a-method-for-a-custom-object-in-javascript "StackOverflow"
[link1]: http://en.wikipedia.org/wiki/Json "Wikipedia Article on JSON"
[link2]: http://www.mongodb.org/ "MongoDb home Page"
[link3]: http://en.wikipedia.org/wiki/YAML "Wikipedia Article on YAML"
[link4]: http://en.wikipedia.org/wiki/XML "Wikipedia Article on XML"
[link5]: http://json.org/ "Official JSON Site"