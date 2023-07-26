#Python Development On Windows#

For absolute beginners setting up Python on Windows can be a bit daunting. There used to be a great guide for setting
up a development environment on the Sad Phaeton Blog which is sadly now defunct. I will try to emulate the brilliance of that guide
but if you want another guide there's one [here][link0].

##Installing Python##

There are currently 2 main releases of Python, Python 2.7 and Python 3.3. There are some differences in the language between the 
two and the support for external libraries is better in 2.7 just by virtue of it being older. We'll use Python 2.7 in this tutorial.

[Download it here][link1], I use 32 bit Python (Python 2.7.5 Windows Installer) because the installers provided for third-party libraries tend
to look for the 32 bit version. You can trick these installers into using the 64 bit version by editing the Registry but unless you're going to be using huge datasets 
32 bit should do for you.

Run the installer using the default settings. I tend to install to the directory (C:/Python27). If we open the Windows command prompt (run "cmd")
and type "python" we get the following response.

<img src = "/images/Python-Environment/python-not-recognized.png" alt = "Command Prompt does not recognize Python"/>

This tells us that Windows has no idea what a "python" is. To educate it we need to change the environment variables and path.

##PATH Environment Variable##

So what is the [Path][link2]? Basically it tells Windows where to look for programs that need to be executed from the command window. The Path is a special type of 
environment variable.

To find out what's in your path type "echo %PATH%" into the command prompt.

<img src = "/images/Python-Environment/path-echo.png" alt = "Command Prompt echos the path"/>

The % signs mark out an environment variable. This type of variable is just a name for some text, so the %PATH% variable
refers to a semicolon (;) delimited list of directories on the Windows system. The reason Windows doesn't know what python is
is because it's not in the path and it can't find it.

Now we come to the fun part! Setting up your own environment variables (disclaimer: no fun will be had). To find your environment
variables go to:

control panel -> system -> advanced system settings -> environment variables

As shown here:

<img src = "/images/Python-Environment/environment-variable-window.png" alt = "The environment variable dialog box"/>

##Python Environment Variable##

The first thing we're going to do to make life easier is give the location of Python an environment variable. Why do this?
When we add Python to the Path text if we hardcode the location and then change that location in the future we'll have to edit the path carefully.
By using an environment variable if we change version (to Python 3.3) in future we can just change the environment variable and the system will still work.

To add the environment variable:

<img src = "/images/Python-Environment/new-py27.png" alt = "Add new environment variable"/>

Add a new system variable (not user variable though you can if you want) called PY27 (or whatever name you want really but I'm using PY27) with the value being
the location of the Python folder "C:\Python27".

After adding this open a NEW cmd window and type "echo %PY27%" (or whichever name you chose) and you should see the name of the directory displayed.
You can also move the command prompt into the Python folder by typing "cd %PY27%:

<img src = "/images/Python-Environment/py27-echo.png" alt = "Command prompt echos new environment variable"/>


##Edit the Path##

We still can't access Python by typing "python" in the command prompt, so what have we achieved? Well, nothing yet!

We need to add the new Python Environment Variable (PY27) to the Path. To do so find the Path System Variable and edit it.
Add %PY27% after a semicolon ';' separating it from the previous entry.

<img src = "/images/Python-Environment/edit-path.png" alt = "Add %PY27% to the Path"/>

Now if we type "python" into the command prompt (open a NEW command prompt each time you edit an environment variable) we should start the python shell:

<img src = "/images/Python-Environment/python-cmd.png" alt = "Running python in command prompt"/>

So, we've added an environment variable for the location of Python and added that to our path.

Now to setup easy_install.

##easy_install##

easy_install is a way to easily install (surprisingly) libraries (bits of code other people have written to make our lives easier).

Download and run the easy_install python script according to [these][link3] instructions. Check it installed correctly by looking in your
Python directory (C:/Python27 for me) at the "Scripts" folder for "easy-install.exe". If you have trouble getting it by this method, the wonderful
Christoph Gohlke provides an [exe installer][link4] for specific versions of Python.

##Add easy_install to Path##

Assuming you have easy_install.exe in your scripts folder let's add it so we can invoke it from command prompt. Reopen your environment variables and edit the
path again to add "%PY27%\Scripts" to the Path variable:

<img src = "/images/Python-Environment/edit-path-2.png" alt = "Edit Path Variable"/>

Remember to open a new command prompt after making these changes. Type "easy_install" and you should get an error message:

<img src = "/images/Python-Environment/easy-install-error.png" alt = "Run easy_install in command prompt"/>

This occurs because we provided no library name for the program to install. 

I realise I've guided you through setting up easy\_install but nowadays we tend to prefer 'pip' the Python package manager. So let's install that using easy\_install. Type
"easy_install pip" and you should see:

<img src = "/images/Python-Environment/easy-pip.png" alt = "easy_install pip in command prompt"/>

You can now install packages using "pip install PACKAGE_NAME" in the command prompt, eg the unit testing library Nose:

<img src = "/images/Python-Environment/pip-install-nose.png" alt = "pip install nose"/>

Now, the main issue with Python on Windows is that Python libraries tend to have extensions written in the language 'C'. Most operating systems come with a 
C compiler included. Windows, for no good reason, lacks a C compiler which causes all sorts of errors with our lovely new system, eg for numpy:

<img src = "/images/Python-Environment/vcvarsall.png" alt = "pip install fails due to vcvarsall.bat"/>

I still haven't got to the bottom of this error and it seems to have [many suggested fixes][link5]. I've so far tried using MinGW but will try Visual Studio when I need to, but for 
most purposes using [the Windows installers (absolute lifesavers)][link6] or some combination of easy\_install and pip will work. Here "easy\_install numpy" works fine:

<img src = "/images/Python-Environment/numpy-install.png" alt = "successful numpy install"/>

[link0]:http://varunpant.com/posts/how-to-setup-easy_install-on-windows "Blog on how to use easy_install on Windows"
[link1]:http://www.python.org/download/ "Python Download Page"
[link2]:http://en.wikipedia.org/wiki/PATH_(variable) "Wikipedia article on Path variable"
[link3]:https://pypi.python.org/pypi/setuptools/1.1#windows "Windows installation of setup tools"
[link4]:http://www.lfd.uci.edu/~gohlke/pythonlibs/#setuptools "Setuptools installer for Windows"
[link5]:http://stackoverflow.com/questions/2817869/error-unable-to-find-vcvarsall-bat "People having the same problem"
[link6]:http://www.lfd.uci.edu/~gohlke/pythonlibs/ "Windows installers for Python Libraries"