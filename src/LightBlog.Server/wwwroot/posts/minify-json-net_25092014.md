# Minify Json using JSON.NET

In my current project I'm using Json stored in the SQL Server database to replicate the storage capability of the browser Session but allowing the data to persist on the user reloading/leaving the page.

One of the challenges is to store the serialized object in as few characters as possible while preserving meaning. 

Without touching the settings, Json.Net stores the strings without extraneous whitespace, but even serializing a fairly small aggregate object can use a lot of characters.This post details a couple of the features I've used to minimise the number of characters.

### Store Booleans as Integers in Json

JSON.NET supports custom conversions using a class which inherits from JsonConverter:

	public class BooleanConverter : JsonConverter
	{
	    public override bool CanConvert(Type objectType)
	    {
	        return typeof(bool).IsAssignableFrom(objectType);
	    }
	
	    public override void WriteJson(JsonWriter writer,
	        object value, JsonSerializer serializer)
	    {
	        bool? source = value as bool?;
	        if (source == null) { return; }
	
	        int valueAsInt = ((bool)source) ? 1 : 0;
	
	        writer.WriteValue(valueAsInt);
	    }
	
	    public override object ReadJson(JsonReader reader, Type objectType,
	        object existingValue, JsonSerializer serializer)
	    {
	        bool returnValue = (Convert.ToInt32(reader.Value) == 1);
	
	        return returnValue;
	    }
	}

This converter will store booleans as 1 and 0 rather than true and false; this saves 3 and 4 characters respectively.

### Shorten Field and Property Names

By using the built in ```[JsonProperty("Name")]``` we can save space in the serialized object. If we define one letter aliases for our properties we save a lot of space, especially when serializing an array of objects:
	
	[JsonProperty("b")]
	public bool IsAThing { get; set; } 

If the full object has ```IsAThing = false``` the string representing the serialized object stores this as ```"b":0``` which is the shortest representation possible (unless you set the default value to false and only serialize where the boolean value is true).


### Serializing an HtmlString

This isn't a space saving tip but where you need ```HtmlString```s to be serialized and deserialized you can use the converter below:

	public class HtmlStringConverter : JsonConverter
	{
	    public override bool CanConvert(Type objectType)
	    {
	        return typeof(IHtmlString).IsAssignableFrom(objectType);
	    }
	
	    public override void WriteJson(JsonWriter writer, 
	        object value, JsonSerializer serializer)
	    {
	        IHtmlString source = value as IHtmlString;
	        if (source == null) { return; }            
	        writer.WriteValue(source.ToString());
	    }
	
	    public override object ReadJson(JsonReader reader, Type objectType, 
	        object existingValue, JsonSerializer serializer)
	    {
	        return new HtmlString((string)reader.Value);
	    }
	}

With thanks to this [StackOverflow answer][link0].

[link0]: http://stackoverflow.com/questions/11350392/how-do-i-serialize-ihtmlstring-to-json-with-json-net "StackOverflow answer for serializing HtmlString"