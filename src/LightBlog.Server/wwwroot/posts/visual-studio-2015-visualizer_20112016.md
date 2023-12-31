# Writing a custom debug visualizer for Visual Studio 2015 #

Often at work I'll be debugging some incredibly complex logic with lists that can be hundreds, if not thousands, of items long. While the ability to inspect these lists using the normal debugging tools is useful, it gets annoying trying to scroll to the 700th item in a list and then accidentally moving the mouse focus away and having to start again.

For this reason I wanted to look into the possiblity of creating a custom display for certain objects while debugging in Visual Studio. For those of you who have worked with the ```DataTable``` you might be familiar with this screen:

![data table visualizer][img0]

This can be accessed by hovering over the object when paused in the debugger and clicking the magnifying glass icon as shown here:

![magnifying glass accessed from debugger][img1]

Visual Studio supports [creating custom screens][link0] like this called **Visualizers**.

### Getting Started ###

The [linked MSDN guide][link0] and similar guides detail how to create the custom screen using WinForms. I wanted to use WPF because it was the desktop UI technology I was most familiar with. 
I followed the [walkthrough][link1] but used this code to display the Visualizer:

    var window = new MainWindow();
    window.ShowDialog();
    
This worked well and my empty WPF window was displayed, but when I closed the window a *CannotUnloadAppDomainException* was thrown and the WPF window reopened:

    System.CannotUnloadAppDomainException was unhandled
    HResult=-2146234347 Message=Error while unloading appdomain. (Exception from HRESULT: 0x80131015) 
    
I spent a couple of hours trying to figure this out and ended up talking to myself on StackOverflow, in the end it turned out it was [due to the Stylus Input thread refusing to close][link2].

The solution to this was to explicitly close the WPF Dispatcher when closing the window:

    protected override void OnClosing(CancelEventArgs e)
    {
        Dispatcher.InvokeShutdown();
    
        base.OnClosing(e);
    }

This closed the stylus input (touchscreen) thread properly and my Visualizer now worked properly.

### Custom Objects ###

Since I wanted to write Visualizers for custom types rather than just the system types, the next step was to experiment with a custom class. For testing I imaginatively created a person class:
    
    public class Person
    {
        public Guid Id { get; private set; }

        public string FirstName { get; private set; }

        public string LastName { get; private set; }

        public string FavoriteColor { get; private set; }

        public DateTime BirthDate { get; set; }

        public Person(string firstName, string lastName, DateTime birthDate, string favoriteColor)
        {
            Id = Guid.NewGuid();
            FirstName = firstName;
            LastName = lastName;
            BirthDate = birthDate;
            FavoriteColor = favoriteColor;
        }

        private Person()
        {
        }
    }

Generally if a class is marked with the ```[Serializable]``` attribute it can be passed directly to your Visualizer. This is because the active object from Visual Studio must be serialized and passed to the debugger side which runs separately, the Visualizer doesn't have access to the original object. When the type is marked with the serializable attribute it can be easily retrieved using the method:

    objectProvider.GetObject();
    
However since a lot of the classes I was working with weren't going to be ```[Serializable]``` I needed to support types without this attribute.

### Non-Serializable types ###

The ```Person``` class above is not marked as ```[Serializable]``` in order to experiment with visualizing non-serializable types.

Since we need to pass objects between the process being debugged and the Visualizer I opted to use JSON.

My Visualizer code was:

    assembly: DebuggerVisualizer(typeof(DebuggerSide), typeof(PersonObjectSource),
        Target = typeof(Person), Description = "Person Visualizer")]
    namespace Visualizer
    {
        public class DebuggerSide : DialogDebuggerVisualizer
        {
            protected override void Show(IDialogVisualizerService windowService, IVisualizerObjectProvider objectProvider)
            {
                try
                {
                    using (var reader = new StreamReader(objectProvider.GetData()))
                    {
                        var result = reader.ReadToEnd();
                        
                        var person = JsonConvert.DeserializeObject<Person>(result, new JsonSerializerSettings
                        {
                            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                            ContractResolver = new PrivatePropertyResolver()
                        });
    
                        if (person == null)
                        {
                            MessageBox.Show("Cannot visualize this object, please use a different visualizer.", "Type Not Supported", MessageBoxButtons.OK, MessageBoxIcon.Information);
    
                            return;
                        }
    
                        var window = new MainWindow(person);
    
                        window.ShowDialog();
                    }
                }
                catch (Exception ex)
                {
                    Trace.Fail(ex.Message, ex.StackTrace);
                }
                    
            }
    
            public static void TestShowVisualizer(object thingToVisualize)
            {
                var visualizerHost = new VisualizerDevelopmentHost(thingToVisualize, typeof(DebuggerSide), typeof(PersonObjectSource));
                visualizerHost.ShowVisualizer();
            }
        }
    }
    
It's worth noting that this handles the case where it's been handled the wrong object type gracefully using a WinForms message box to inform the user the object is wrong:

    if (person == null)
    {
        MessageBox.Show("Cannot visualize this object, please use a different visualizer.", "Type Not Supported", MessageBoxButtons.OK, MessageBoxIcon.Information);

        return;
    } 

This turns out to be useful for a List visualizer where, even if only want to provide visualization for a single type of object, you can't specify the type of T in a Visualizer for ```List<T>``` so you must handle Visualization requests for the wrong type.

In order to provide the ```Person``` to this Visualizer we must write it to the stream. We do this by specifying that our Visualizer uses a ```PersonObjectSource```. This class inherits from the normal ```VisualizerObjectSource``` but overrides ```GetData``` to use JSON serialization instead:

    public class PersonObjectSource : VisualizerObjectSource
    {
        public override void GetData(object target, Stream outgoingData)
        {
            var person = target as Person;

            if (person == null)
            {
                return;
            }

            var writer = new StreamWriter(outgoingData);

            var content = JsonConvert.SerializeObject(person);

            writer.WriteLine(content);

            writer.Flush();
        }
    }

Again we use the ```as``` operator here to handle the case where we get an unexpected or null object type.

Now we have our person object in our Visualizer we can use it in our WPF window and write a normal WPF application. For this example I put together a terrible UI:

![person visualizer showing name and favorite color][img2]

### Lists ###

I also wanted to support Visualization of lists of a certain type. Unfortunately it's not possible to specify the type of ```List<T>``` you wish to provide Visualization support for so Visual Studio will provide your visualizer for all Lists. 

Since the Visualizer I was intending to write would be internal to the company it would be acceptable to handle the case where the List was of the wrong type by displaying a message box, but this would be annoying to general users.

To specify that your Visualizer supports Lists you simply specify this in the assembly attribute:

    [assembly: DebuggerVisualizer(typeof(DebuggerSide), typeof(VisualizerObjectSource),
        Target = typeof(List<>), Description = "My List Visualizer")]
        
And then handle being passed the wrong type in the ```Show``` method.

### A more advanced case ###

I am terrible at both pronouncing and writing Regexes (Ree-gexes?) and struggle with cases more complex than ```.*```. My favourite site for writing Regexes is [Regex Storm][link3] since it is based on the .NET regex engine and provides a clean, simple UI.

Despite having that site available I still struggle with getting Regexes right, especially when using named Groups.

For that reason to practice writing Visualizers I put together this basic Visualizer for a ```MatchCollection```:

![regex visualizer showing matches, groups and captures][img3]

The full code for this Visualizer and the simpler Person Visualizer can be found [here][link4].

### Installation ###

In order to install a Visualizer the .dll simply needs to be placed in (assuming a default Visual Studio installation):

    C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\Packages\Debugger\Visualizers

You will need an administrator account to move files into this folder.

[link0]: https://msdn.microsoft.com/en-us/library/e2zc529c.aspx "how to: write a visualizer"
[link1]: https://msdn.microsoft.com/en-us/library/ms164759.aspx "walkthrough: writing a visualizer"
[link2]: http://stackoverflow.com/questions/40685957/visualizer-an-unhandled-exception-of-type-system-cannotunloadappdomainexceptio/40687053 "stackoverflow: cannot unload appdomain exception"
[link3]: http://regexstorm.net/ "Regex Storm - Online .NET regex"
[link4]: https://github.com/EliotJones/VS2015Visualizer "source on github"
[img0]:https://eliot-jones.com/images/Visualizer/data-table-visualizer.png
[img1]:https://eliot-jones.com/images/Visualizer/magnifying-glass.png
[img2]:https://eliot-jones.com/images/Visualizer/person-visualizer.png
[img3]:https://eliot-jones.com/images/Visualizer/regex-visualizer.png