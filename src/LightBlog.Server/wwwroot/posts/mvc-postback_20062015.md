# MVC postback for users without JavaScript - Post 1 #

Posts in this series:

+ **Post 1**
+ [Post 2][link0]

Despite only around 1.2% of users having JavaScript disabled there is still a requirement to develop sites which work for users who have JavaScript turned off, especially sites which provide government services. The UK's Government Digital Services (GDS) require that a site works without JavaScript and is then enhanced using JavaScript. This causes all sorts of problems designing forms, especially where there is an "Add more" option for input fields.

Designing something like an "Add more" functionality with JavaScript is fairly trivial, having it work for those without is less so. Having encountered this problem several times I've written a summary of the approach I have used to address this.

### The code ###
All the code for this tutorial is available on [GitHub][link1]. I have used Git's branches to take snapshots of development changes. The final code is in the branch ["step-5"][link2]. Additonally I have hosted the website on Azure so you can see what the final page does [here][link3].

### The product ###
We are designing a page to allow users to choose items from a menu, once they have selected as many items as they want, they submit the form and are taken to a summary of their active orders:

![The screen features dropdowns containing menu items and an add another button allowing the user to add more input dropdowns](https://eliot-jones.com/images/postback/final-screen.png)

### Getting started ###
The postback concept is from ASP.NET Webforms and there's no real parallel in MVC, which is a good thing because the postback model encouraged ignorance of a site working with HTTP in a stateless manner. However it is still possible to create a postback style model in MVC.

We start with a simple almost empty MVC web application project with a Home controller and a Menu controller:

    public class MenuController : Controller
    {
        [HttpGet]
        public ActionResult Order()
        {
            return View();
        }
    }

There's also a Meal model in the Models folder:

    public class Meal
    {
        public int Id { get; set; }

        public decimal Price { get; set; }

        public string Name { get; set; }

        public int NumberRemaining { get; set; }

        public decimal TimeToServe { get; set; }
    }

And a class called Menu which provides a list of different meals and the ability to order them.

### Step 1 ###

[The first step][link4] is to create a form to allow the user to select one meal and post to the server. We use a ViewModel for data binding to our view:

    public class OrderViewModel
    {
        public SelectList MenuItems { get; set; }

        [Required(ErrorMessage = "Please select a menu item.")]
        public int? SelectedMenuItem { get; set; }

        public IList<int> SelectedItems { get; set; }

        public OrderViewModel()
        {
            this.SelectedItems = new List<int>();
        }
    }

This has a corresponding Get and Post action on the [controller] and the following view:

	@{
	    ViewBag.Title = "Order";
	}
	
	<h2>Order</h2>
	
	@using (Html.BeginForm())
	{
	    @Html.ValidationSummary()
	
	    <div class="form-group">
	        @Html.LabelFor(m => m.SelectedMenuItem, "Select a menu item")
	        @Html.ValidationMessageFor(m => m.SelectedMenuItem)
	        @Html.DropDownListFor(m => m.SelectedMenuItem, Model.MenuItems, "Please select", new {@class = "form-control"})
	    </div>
	    <button type="submit">Submit</button>
	}

Currently this view has one submit button which posts the entire form.

### Step 2 ###

[The next step][link5] adds a second submit button to the view:

    <div class="form-group">
        @Html.LabelFor(m => m.SelectedMenuItem, "Select a menu item", new { @class = "dropdown-label" })
        @Html.ValidationMessageFor(m => m.SelectedMenuItem)
        @Html.DropDownListFor(m => m.SelectedMenuItem, Model.MenuItems, "Please select", new {@class = "form-control dropdown"})
        <button type="submit" name="submit" value="add"><span class="glyphicon glyphicon-plus"></span>Add another</button>
    </div>
    <button type="submit" name="submit" value="submit">Submit</button>

Both buttons now have a name attribute (submit) and different value attributes, "add" and "submit" respectively.

We can now detect which of the two buttons have been clicked in the controller action and do different things for either button. Currently our controller submits the form for both buttons.

### Step 3 ###

[Next][link6] we set the controller to treat a post sent using the add button differently to one sent using submit:

	[HttpPost]
    public ActionResult Order(OrderViewModel model, string submit)
    {
        if (model.SelectedMenuItem.HasValue)
        {
            model.SelectedItems.Add(model.SelectedMenuItem.Value);
        }

        BindSelectLists(model);
        
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(submit))
        {
            return View(model);
        }

        switch (submit)
        {
            case AddValue:
                return AddPostback(model);
            default:
                return SubmitPostback(model);
        }
    }



The controller action takes a string parameter with the same name as the submit buttons, the value of this will either be "add" or "submit", the values we set for the buttons in our HTML.

The action first adds the currently selected item to the list of previously selected items if present. After checking that the ModelState is valid (i.e. that there is a currently selected item) it chooses one of two methods to call depending on the button pressed. There is a method for the add post and the submit post.    

	public ActionResult AddPostback(OrderViewModel model)
    {
        model.SelectedMenuItem = null;

        return View("Order", model);
    }

    public ActionResult SubmitPostback(OrderViewModel model)
    {
        foreach (var id in model.SelectedItems)
        {
            Meal meal = Menu.Single(m => m.Id == id);

            Menu.Order(meal);
        }

        return RedirectToAction("Summary");
    }

+ Add: Sets the currently selected item to null and returns the view.
+ Submit: Orders the items and moves to the summary screen.

It's important to note that this approach goes against the way the MVC framework is designed, [by design][link7] MVC treats the return of a view (rather than a redirect) from an HTTP POST as being due to Validation errors.

In this step there have been some changes to the view to provide [partial views][link8] showing the current selection input and previously selected items.

Currently we have a problem, when we click "Add another" and the page performs a postback, the current input still loads with the previous value selected. Even though we set the selected value to null in the ```AddPostback``` method and it loads in the view as null it's not resetting in the view. Somehow MVC has cached the value for this dropdown.

![The current dropdown has the same value as the previous.](https://eliot-jones.com/images/postback/cache-problem.png)

We will address this problem in the [next post][link0].

[link0]: http://eliot-jones.com/2015/06/mvc-postback-2 "Second post in this series"
[link1]: https://github.com/EliotJones/PostbackTutorial "All code for these tutorials on GitHub"
[link2]: https://github.com/EliotJones/PostbackTutorial/tree/step-5/EliotJones.PostbackTutorial "The code for the last step on GitHub"
[link3]: http://postbacktest.azurewebsites.net/ "The product hosted on Azure"
[link4]: https://github.com/EliotJones/PostbackTutorial/tree/step-1/EliotJones.PostbackTutorial "The code for the first step on GitHub"
[link5]: https://github.com/EliotJones/PostbackTutorial/tree/step-2/EliotJones.PostbackTutorial  "The code for the second step on GitHub"
[link6]: https://github.com/EliotJones/PostbackTutorial/tree/step-3/EliotJones.PostbackTutorial  "The code for the third step on GitHub"
[link7]: http://blogs.msdn.com/b/simonince/archive/2010/05/05/asp-net-mvc-s-html-helpers-render-the-wrong-value.aspx "An MSDN blog post explaining how rendering views on POST is different"
[link8]: https://github.com/EliotJones/PostbackTutorial/tree/step-3/EliotJones.PostbackTutorial/Views/Menu "The code for the menu view on GitHub"