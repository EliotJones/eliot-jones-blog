# Automated web UI testing using Selenium with Python #

Legacy applications are a fact of life in software development. However well a piece of software starts and no matter how much care is taken to document it, as people leave and new people are assigned to look after it, knowledge about what the software does or how it's meant to work is lost.

For older code which was written before a time of TDD and testable architecture this creates a fear of change, both for a developer and for the business. Testing is time consuming and manual and the knowledge of what the expected inputs and outputs of the application are, are lost and changed as ownership of the application changes hands.

In his book [Working Effectively with Legacy Code][legacy] Michael Feathers outlines steps which can be taken inside the code to refactor it and start adding test coverage. Depending on the size of the codebase and the budgetary and time constraints it can be difficult to get very far with refactoring the codebase itself.

## A useful tool ##

While automated tests should also be used for new software they can be just as, if not more, useful when it comes to legacy software. For example, the UI of a legacy application tends to be much more fixed than for a project currently undergoing development.

Automated UI tests for a web application can provide a useful time-saver and allow you to quickly cover the application with regression tests at a higher level. 

One of the most popular tools for automated UI testing is [Selenium][selenium]. Selenium offers support for a range of languages, including .NET, Java, Ruby, etc. With Selenium you can quickly script interaction with different browsers, primarily Firefox but there are also drivers available for Internet Explorer, Google Chrome and Phantom.

## A natural fit ##

While the concept of Business Driven Development (BDD) is orthogonal to automation testing, the two work well together. 

Even though BDD is about more than just a diferent way of coding, the Gherkin style Given, When, Then syntax is a good fit for UI tests. Since these Gherkin requirements often represent the outcome the user is expecting to see when they use the application they tend to align more naturally with UI testing.

As an example of a business requirement written in the Gherkin style (I may have my Given and When the wrong way round in terms of the meaning of the steps):

	Given a user requests access
	When I am administrating the system
	Then I can deny the access request

## Using it ##

I've been playing around with the Python [behave][behave] library and the Selenium bindings for Python. While I've written Selenium tests in .NET before this seems like overkill for what is effectively scripting a test suite. Python as a language seems a nicer, more lightweight, fit for Selenium with BDD and it ticks those nice open source boxes.

Since early project set-up can be an issue for adopting a UI testing approach I decided to make a [reusable project template][github] which can be used and extended for building UI tests. The nice thing about this project is as long as you have Python (3.5+) and a web browser on your system you can download it and have it running almost instantly.

This means you can start developing your features and steps without having to write any set-up code.

The project comes with one feature containing two demo scenarios:

	Feature: Load Wikipedia home
	
	Scenario: Load it
		Given I am not logged in
		When I am on the homepage
		Then I should see the English option
	
	Scenario: Search Cats
		Given I am on the homepage
		When I search cats
		Then I should go to the cats page  
	
All the plumbing responsible for changing browsers in the configuration and easily accessing your own additional configuration settings from code is taken care of.

As a result you can just write your features and steps without worrying about anything else. Here are the steps (except the "I am on the homepage" step which is in the shared steps) for the above feature:
	
	import behave
	from selenium import webdriver
	from helpers import configuration
	from selenium.webdriver.common.keys import Keys
	
	@given("I am not logged in")
	def step_impl(context):
	    pass
	
	@then('I should see the English option')
	def step_impl(context):
	    link = context.browser.find_element_by_link_text("English")
	    assert True is True
	 
	@when("I search cats")
	def step_impl(context):
	    search = context.browser.find_element_by_id("searchInput")
	    search.send_keys("cats")
	    search.submit()
	    
	@then("I should go to the cats page")
	def step_impl(context):
	    title = context.browser.find_element_by_tag_name("h1")
	    assert "Cat" in title.text 

The project also offers:

+ Support for Internet Explorer, Chrome and Firefox
+ Screenshots on test failure
+ Configurable deletion of cookies to take place after Steps, Scenarios, Features or the entire test run
+ A single script to install all dependencies and create a isolated Virtual Environment to avoid polluting your global installation
+ Reporting of tests results in the JUnit format.
 
If you don't want to use Git you can just download the project as a zip and use it as the base for your development. Thanks to the install script it's also easy to integrate with Jenkins.

The best documentation for using the Selenium driver with Python is [here][seleniumdocs].

Go [give it a go][github] and let me know how it goes :)

I'm hoping to extend it with support for Phantom as well as more utility functions for quickly selecting from a dropdown, etc.

[legacy]:http://www.amazon.co.uk/Working-Effectively-Legacy-Michael-Feathers/dp/0131177052
[selenium]:http://www.seleniumhq.org/
[behave]:http://pythonhosted.org/behave/index.html
[seleniumdocs]:http://selenium-python.readthedocs.org/
[github]:https://github.com/EliotJones/behave-selenium-template