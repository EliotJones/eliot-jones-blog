#AOB (1)

AOB here is used to mean 'any other business' rather than the Swedish pop group Ace of Base or the Accessory olfactory bulb.

This a blog post to summarise a few things which don't make a whole blog post in their own right.

###MarkdownPad 2

----------

For those of you who don't know, [markdown][link0] is readable HTML syntax so you can write properly structured HTML in a human friendly way.

The conversion to HTML is then performed by the markdown library in your language of choice using a mega-regex.

The format is used by sites like StackOverflow and GitHub for comments/text and I also use it for my blog posts.

So after Scott Hanselmann mentioned it in his blog I downloaded [MarkdownPad 2][link1].

Beforehand I was using Notepad++ to write Markdown and it was horribly long winded due to the poor choice of application. Now writing Markdown is about as easy as using Word (so go download it if you use markdown).

###Ceci n'est pas une pipe (trans. C# Equality is Weird)


----------

I was trying to write a method to find where properties had been set back to their default value on an object. Reflection seemed like an obvious choice since I wanted a one-size fits all method.

I ran into a very odd problem where C# was convinced that ```0 == 0``` is false, or so I thought. The problem was based on my lack of understanding of how the equality operator actually works, but provided a good learning experience.

The actual comparison that troubled me could be simplified to:

    int num = 0;
    object num1 = num;
    object num2 = num;
	num1 == num2 //false

See [this StackOverflow question][link2] for the answer. Basically for the ```object``` type, ```==``` checks that both objects occupy the same location in memory (ReferenceEquals).

For the example above the integer is boxed (converted to the reference type ```object```) in two different locations so ```==``` returns false since they occupy different locations in memory.

If ```Equals(num1, num2)``` is used instead, the method returns true.

This lead to the even more interesting problem posed by Eric Lippert in the comment on the question. Why does the following occur (this is output from Immediate Window in Visual Studio)?

    short myshort = 0;
    0
    int myint = 0;
    0
    
    myshort.Equals(myint)
    false
    myint.Equals(myshort)
    true
    myint == myshort
    true

For the full explanation see [his post on Coverity][link3] which explains what's going on in detail. There's also this [post by Jon Skeet][link4] for further reading.

###ActionLink MVC


----------

Just for future reference here's an ActionLink from MVC4:

	@Html.ActionLink("Edit", "Edit", "ControllerName", 
		routeValues: new { first="duck", @Model.Id, other = "rgs", thing = @Model.Id}, 
		htmlAttributes: new { @class = "cssclass" })

The first parameter is link text, second is Action Method and third is controller name.


Route values are added as an anonymous type. They are compared against the defaults in RouteConfig.cs and any that do not match the pattern will be added in the querystring. For example:

	routes.MapRoute(
	                name: "Default",
	                url: "{controller}/{action}/{id}",
	                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
	            );
Will map 'other', 'first' and 'thing' into the querystring, whereas the unnamed value will be the id.

	{more url here}/Edit/-2147483647?first=duck&other=rgs&thing=-2147483647

###Sea Kayaking


----------

Just in case concerned readers were thinking I spent my entire time coding, I've uploaded a couple of pictures from my recent kayaking trip to Old Harry Rocks. I've got a sit on-top kayak [from GoSea][link5] which is great for exploring the coast but heavy-going once the wind picks up. 

<img src="/images/aob/setting-sail.JPG" alt="Setting Off For Old Harry Rocks" />

In total the distance was about 7km there and back, you can see Old Harry [in the background here][link6].

I've also recently taken it to Lulworth and paddled along to Durdle Door which was great and highly recommended if you get a chance.

[link0]: http://daringfireball.net/projects/markdown/ "John Gruber Markdown"
[link1]: https://markdownpad.com/ "Official Download Site"
[link2]: http://stackoverflow.com/questions/20642202/why-is-object0-object0-different-from-object0-equalsobject0 "Question"
[link3]: http://blog.coverity.com/2014/01/13/inconsistent-equality/#.U-dYYfldXQg "Eric Lippert on inconsistent equality"
[link4]: http://blogs.msdn.com/b/csharpfaq/archive/2004/03/29/when-should-i-use-and-when-should-i-use-equals.aspx "MSDN Blog Post by Jon skeet"
[link5]: http://www.gosea.co.uk/kayaks/#.U-dvu_ldXQg "GoSea Kayaks"
[link6]: /images/aob/old-harry-rocks.JPG