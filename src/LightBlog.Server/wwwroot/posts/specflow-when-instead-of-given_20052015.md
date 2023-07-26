# SpecFlow looking for When instead of Given step #

When writing some automation tests using Selenium with SpecFlow recently I was faced with an odd error:

		No matching step definition found for the step. Use the following code to create one:
        [When(@"I add a new dog")]
        public void WhenIAddANewDog()
        {
            ScenarioContext.Current.Pending();
        }

The suggested code was using a ```[When("My Scenario")]``` step despite the feature file declaring it as a ```Given```:

	Scenario: Check for existing dog
		Given I have a new Dog Controller
		And I add a new dog
		When I check if the added dog exists
		Then The check is true

(This feature is written badly to illustrate the error).

The step it couldn't find was the ```And I add a new dog``` step. Despite showing as bound in the feature file and being able to navigate to the step definition, the running test couldn't find it and was looking for a ```When``` instead of a ```Given```.

This is because the previous step was calling sub steps as follows:

    public class DogControllerSteps : Steps
    {
        [Given("I have a new Dog Controller")]
        public void CreateDogController()
        {
            Given("I have a new query bus");

            When("I create a new dog controller");
        }
	}

Inheriting from ```Steps``` allows us to reuse step definitions from the same or other step files.

When SpecFlow runs this it knows that it last ran a ```When``` step, despite being in the definition of a ```Given```. Therefore when the next step is defined with ```And```, it looks for a ```When```. To fix this you can simply change your feature to be more specific:

	Scenario: Check for existing dog
		Given I have a new Dog Controller
		Given I add a new dog
		When I check if the added dog exists
		Then The check is true

Passing tests :)