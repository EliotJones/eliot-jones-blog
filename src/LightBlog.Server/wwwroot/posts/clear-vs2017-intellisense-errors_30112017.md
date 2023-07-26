# Visual Studio 2017 Red Underline/Incorrect Highlights #

There are a lot of answers on this topic but in order to aggregate the steps I usually follow for future reference I'm noting them in this blog post.

There are few things more annoying than Intellisense going wobbly and flagging successfully compiling code with errors. Obviously the first step is to restart Visual Studio but if the problem persists you need to try something more. These steps are for Visual Studio 2017 Community with Resharper.

You can check whether the underlines have disappeared after each step or run them all:

1. Unload then reload the problematic project from Solution Explorer. To do this, right click the project, select ```Unload Project``` and then ```Reload Project```. This sometimes helps clear incorrect highlighting due to Resharper especially after merges.
2. Clear the Resharper cache. This is accessed by going to Resharper > Options > General > Clear caches in the menu. You will need to restart Visual Studio to see if this step worked.
3. Disable Resharper from Tools > Options > Resharper > Suspend Now. Then start it again from the same location.
4. Close Visual Studio and then delete the ```.vs``` folder from the source folder. This is a hidden folder at the same level as the .sln file.
5. With Visual Studio closed delete the ```obj``` folders from the problematic project folders.