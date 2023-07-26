# MVC postback for users without JavaScript - Post 2 #

Posts in this series:

+ [Post 1][link1]
+ **Post 2**

So far we have created a form which performs a postback to the server to add more input fields to the same page. We have encountered an issue with MVC seeming to cache partial views (or at least the values inside them).

### Step 4 ###

[Step 4][link2] attempts to fix the caching of the selected value field.

I spent hours looking for a way to stop MVC caching partial views, which is what I thought was happening. I tried disabling the output cache and loading the partial views via actions, neither of which helped. I finally found [this answer][link3] on StackOverflow; the ModelState holds the old values when a view is reloaded via an HTTP POST even if you change the values of the model in the action.

By removing the specific value from the ModelState the value clears properly and the view displays as expected.

    private ActionResult AddPostback(OrderViewModel model)
    {
        ModelState.Remove("SelectedMenuItem");
        model.SelectedMenuItem = null;

        return View("Order", model);
    }

The form now works as expected, the "add another" button adds another input to the page by reloading the page, the submit button still submits the form to the server and redirects to the summary screen. Now we have the screen working for users without JavaScript however users get a "screen flicker" due to reloading the page when clicking "Add another".

[See this image][link4] for an example of the screen flicker non-JavaScript users will see, this seems like an acceptable compromise, since there's no way to prevent the page having to reload for these users. However for people with JavaScript enabled it would be nice to offer a cleaner user experience.

### Step 5 ###

[This step][link5] mainly focuses on using jQuery Ajax to submit the form in a user friendly way. I originally started using the Microsoft Ajax ```@Ajax.Helper()``` methods but it turned out to be easier to just write the JavaScript in this case.

The first main change is that the body of the form now lives in a new partial view (_OrderPartial) which contains the Current and Previous input partial views:

	@model EliotJones.PostbackTutorial.ViewModels.Menu.OrderViewModel

    @Html.ValidationSummary()
    @Html.AntiForgeryToken()
    @Html.Partial("_Previous", Model)
    @Html.Partial("_Current", Model, ViewData)

The button for "Add another" in the ```_Current``` partial view now calls a JavaScript function using the onclick attribute:

	<button type="submit" name="submit" value="add" onclick="addMore(event);">
		<span class="glyphicon glyphicon-plus"></span>
		Add another
	</button>

Note that we pass ```event``` to the onclick attribute, this is required for the code to work on Firefox.

The code for this JavaScript function is in the Order view:

	@model EliotJones.PostbackTutorial.ViewModels.Menu.OrderViewModel
	<h2>Order</h2>
	<hr/>
	
	@using (Html.BeginForm("Order", "Menu", FormMethod.Post, htmlAttributes: new { id = "form" }))
	{
	    <div id="form-for-ajax">
	        @Html.Partial("_OrderPartial", Model)
	    </div>
	    <button type="submit" name="submit" value="submit">
			Submit
		</button>
	}
	
	@section scripts{
	    <script>
	        function addMore(event) {
				// The code goes here
	        }
	    </script>
	}

The code uses the jQuery Ajax post function:

    function addMore(event) {
        $.post('@Url.Action("Order", new { submit = "add" })', $('#form').serialize())
            .done(function (data) {
                $('#form-for-ajax').html(data);
            }).fail(function () {
                return true;
            });

        event.preventDefault ? event.preventDefault() : event.returnValue = false;
    }

The first parameter of the call to ```$.post()``` is a the URL to post to for this request. I've used the Razor helpers here to avoid hard-coding the entire URL. Since the value of ```submit``` will no longer come from the button we add it as a QueryString parameter:

	$.post('@Url.Action("Order", new { submit = "add" })',

The single quotes ensure the URL helper value is a string in the JavaScript, miss them out at your peril.

The next argument to ```$.post()``` is the data to submit to the URL, this uses jQuery's Serialize to send the form data in the format required.

After a call to ```$.post(url, data)``` the code will continue to the next line, this is not the line you think it might be. Because the post runs asynchronously the next line which runs is:
	
	event.preventDefault ? event.preventDefault() : event.returnValue = false;

This stops the browser submitting the form on click of the submit button in a similar way to ```return false;```. This means for users with JavaScript this button will instead call an Ajax request whereas for users without the form will still submit (because none of this JavaScript code will run).

The ```done``` function does not run immediately because it is a "promise", which means it executes asynchronously. When the Ajax request completes successfully the code in ```done``` will run, alternatively if there's an error then ```fail``` will run.

	$.post(url, submittedData)
        .done(function (data) {
            $('#form-for-ajax').html(data);
        })

The data passed to the function in ```done``` is the data returned by the request, in our case this is HTML, so we can just replace the content of the form with the returned data (using ```.html()```) since the postback just returns the form with the extra input fields.

The problem now is that the controller code currently returns the entire form including layout again so we end up with nested forms:

 ![The postback inserts everything including the page header into the form region.](https://eliot-jones.com/images/postback/nested-screens.png)

### Returning partial views when required ###

Luckily for us MVC 5 offers a convenience method for detecting Ajax requests:

    if (Request.IsAjaxRequest())
    {
        return PartialView("_OrderPartial", model);
    }

This looks for the ```X-Requested-With: XMLHttpRequest``` header used by jQuery Ajax requests ([mainly for security reasons, e.g. CSRF][link6]) and can be used to return the partial view for these JavaScript requests. This check is added in two places. [See the full code][link7] for the finished Menu controller.

    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Order(OrderViewModel model, string submit)
    {
        if (model.SelectedMenuItem.HasValue)
        {
			model.SelectedItems.Add(model.SelectedMenuItem.Value);
        }

        BindSelectLists(model);

        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(submit))
        {
            if (Request.IsAjaxRequest())
            {
                return PartialView("_OrderPartial", model);
            }
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

    private ActionResult AddPostback(OrderViewModel model)
    {
        ModelState.Remove("SelectedMenuItem");

        model.SelectedMenuItem = null;
        if (Request.IsAjaxRequest())
        {
            return PartialView("_OrderPartial", model);
        }
        return View("Order", model);
    }

### Summary ###

In summary:

1. Create the form and controller action with one submit button.
2. Add another submit button with a name and a different value.
3. You can use a parameter to the controller action with the name of the button to check the value of where the submit came from.
4. Return the correct ActionResult for your desired action, watching out for caching in the ModelState.
5. Add an Ajax request to the onclick of the submit button which calls the same controller action but returns partial views instead.

[link1]: http://eliot-jones.com/2015/06/mvc-postback "The first post in the series"
[link2]: https://github.com/EliotJones/PostbackTutorial/tree/step-4/EliotJones.PostbackTutorial "The code for the fourth step on GitHub"
[link3]: http://stackoverflow.com/questions/7414351/mvc-3-html-editorfor-seems-to-cache-old-values-after-ajax-call/7449628#7449628 "How to stop MVC caching values in partial views"
[link4]: https://eliot-jones.com/images/postback/screen-flicker.gif "A gif showing the screen flicker the user sees when the page posts back"
[link5]: https://github.com/EliotJones/PostbackTutorial/tree/step-5/EliotJones.PostbackTutorial "The code for the last step on GitHub"
[link6]: http://stackoverflow.com/questions/17478731/whats-the-point-of-x-requested-with-header/22533680#22533680 "The reason the X-Requested-With header is sent with Ajax requests"
[link7]: https://github.com/EliotJones/PostbackTutorial/blob/step-5/EliotJones.PostbackTutorial/Controllers/MenuController.cs "The code for the final Menu controller on GitHub"