# How it works: Selenium #

I've done quite a bit of work with Selenium on and off, mainly for browser automation tests. There are many high quality official libraries for Selenium covering a range of languages. It's an amazing technology and a real joy to work with; however sometimes stuff can go wrong and it's useful to know what's going on under the hood in order to debug where the problem lies.

For this reason I present my very-probably-wrong guide to Selenium.

### Gecko, drivers!? What's going on? ###

There are 3 components in code that interacts with Selenium (ignoring ```RemoteWebDriver``` use cases):

1. Your code
2. A driver
3. The browser

For each browser that supports Selenium, the browser vendor provides a "driver". This is usually a small executable (.exe) program tailored for a specific browser:

- [Chrome / Chromium](https://sites.google.com/a/chromium.org/chromedriver/ "Google Chrome / Chromium")
- [Firefox](https://github.com/mozilla/geckodriver "Firefox")
- [Edge](https://developer.microsoft.com/en-us/microsoft-edge/tools/webdriver/ "Edge")
- Other drivers available

This driver is the part your code actually interacts with. The driver then communicates with the browser using whatever magic the vendors design. In order to change browsers while using the same code, the drivers expose interfaces/endpoints that implement the [WebDriver specification](https://w3c.github.io/webdriver/webdriver-spec.html "WebDriver W3C Spec").

Put simply, this means the drivers are a simple RESTful API.

### Demo ###

To demonstrate this I'll use the ChromeDriver [version 2.29](https://chromedriver.storage.googleapis.com/index.html?path=2.29/ "ChromeDriver version 2.29") which is the latest version at the time of writing. Drivers can only talk to the versions of the browser they are compatible with. In this demo I'm running Chrome on version 58.

Once you have downloaded the driver you get a simple program called chromedriver.exe. If you double click the exe it launches a console window with some information about the driver version you are running and what port it's listening on:

![The driver logs its version and the port it's listening on](https://eliot-jones.com/images/selenium1/chromedriver-launch.png)

On start-up the driver logs the message:

	Starting ChromeDriver 2.29.461591 (62ebf098771772160f391d75e589dc567915b233) on port 9515

This tells us we can access the driver at the URL ```http://localhost:9515```.

Now that the driver is listening we can ask it to launch a browser instance for us. To do this we need to send an HTTP **POST** request to the following URL:

	http://localhost:9515/session

[This method is defined in the specification](https://w3c.github.io/webdriver/webdriver-spec.html#new-session "W3C spec for new session"). We also need to provide some extra information about the capabilities we want from the browser instance. To do this we pass JSON in the request body.

For a blank set of capabilities the JSON looks like this:

	{
		"desiredCapabilities": {}
	}

To interact with the driver using HTTP requests I am going to use PowerShell since it's installed on all Windows systems. You can use Postman or Fiddler or curl or whichever client you want.

After launching PowerShell we define the JSON for the request body and then send the request using PowerShell's ```Invoke-RestMethod```:

	$body = @{
		desiredCapabilities = @{}
	}
    
	# Change the body object to something we can use to send a request
	$json = $body | ConvertTo-Json

	# Send the actual request
	$response = Invoke-RestMethod -Method Post -Uri http://localhost:9515/session -Body $json

After sending the last line, a new browser window should open. If you don't see the window try typing ```$response.value``` into the PowerShell window and hitting enter. This should contain a message telling you what went wrong.

This window has a unique ```SessionId``` we use to interact with it specifically. We can display this id and assign it to a variable using PowerShell:

	# Store the id
	$sessionId = $response.sessionId

	# Show us what it is
	Write-Host $sessionId

For Chrome the session Id is some hash, for example ```3c0802805fbec93515dacdcc2e6bba72```. For Edge it's a GUID.

Now we have a blank window, the next step is to navigate to a web page. [From the spec](https://w3c.github.io/webdriver/webdriver-spec.html#go "Navigation from the spec") we need to hit the URL ```http://localhost:9515/session/{session id}/url``` where ```{session id}``` is the actual id of the session we created (and helpfully stored in the ```$sessionId``` variable). We also need to tell the driver what URL we want to navigate to. Again we provide this information in the request body as JSON:

	{ "url" : "https://w3c.github.io/webdriver/webdriver-spec.html#go" }

To achieve this in PowerShell we do the following:

	$body = @{ url = "https://w3c.github.io/webdriver/webdriver-spec.html#go" }

	$json = $body | ConvertTo-Json

	# PowerShell will replace $sessionId in the URL with the actual session Id
	$response = $response = Invoke-RestMethod -Method Post -Uri http://localhost:9515/session/$sessionId/url -Body $json

After the last line the browser should navigate to the page in the specification. Finally let's find and click a button. Based on the information from the specification:

    # Define how we want to search for the element, in this case the element with the id "respec-pill"
	$json = @{
		"using" = "id"
		"value" = "respec-pill"
	} | ConvertTo-Json

	# Find the element using the search parameters defined above
	$response = Invoke-RestMethod -Method Post -Uri http://localhost:9515/session/$sessionId/element -Body $json

	# Store the ID of the located element
	$elementId = $response.value.ELEMENT

	# Click the element using the ID we found, no need to post any JSON for this call
	$response = Invoke-RestMethod -Method Post -Uri http://localhost:9515/session/$sessionId/element/$elementId/click

Hopefully this shows you that the Selenium interaction that occurs when your code uses a library for Selenium is actually (fairly) simple. The complicated and magic part happens when the driver talks to the browser.

Some key parts which can go wrong are:

- Your code is using a version of the Selenium library which is sending requests that the driver doesn't understand.
- Your driver is the wrong version to interact with the version of the browser.
- The driver you are using has a bug or does not yet support a feature of the WebDriver specification (for example Edge supports a quite limited subset at the moment).
- Your code has a bug.

Of these, the last item is the absolute, 99.9%, most likely culprit for any bug. Followed by a version mismatch between any 2 of the components. Using your knowledge of how to interact with the driver without any intermediate code you can now perform an extra check to verify where the problem likely lies.