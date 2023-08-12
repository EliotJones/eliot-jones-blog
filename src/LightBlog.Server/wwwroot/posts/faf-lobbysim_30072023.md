# Writing Code for Fun and ... That's It

Supreme Commander: Forged Alliance is a real-time strategy (RTS) game released in 2007. It is also the last good RTS, and potentially game, ever.

Despite this most gamers &#8212; not realizing gaming reached a pinnacle in 2007 and has since descended into a mess of RPGification, Loot Boxes and bloom over-use &#8212; have moved on to other games and more importantly other genres<sup>1</sup>.

One need only look at the modern-game design horrors unleashed on the Dawn of War genre to see the decline of RTS and gaming generally more clearly. From the high-point of Dawn of War: Winter Assault in 2005 to the best-forgotten mess of Dawn of War III in 2017 everything has been ruined irrevocably. Nothing is good anymore, food doesn't taste the same, music isn't what it was<sup>2</sup>. I'm not getting old, things are getting worse. I'm still cool and relevant!

Few things in gaming quite match the state of stressed, overwhelmed, misery induced by Forged Alliance. Thankfully the [Forged Alliance Forever](https://www.faforever.com/) (FAF) project has continued to develop Forged Alliance, providing graphical updates, balance changes, matchmaking, performance improvements, new maps and more.

Because gamers have departed from the True Path matchmaking generally takes a while. A small active user-base means that depending on the time of day and day of week you can be waiting anywhere from 10 minutes to over an hour for a lobby (the pre-game matchmaking bit) to fill.

It is generally necessary to be ready to balance and start the lobby within a few minutes of it filling. If there's too much of a delay everyone will leave again and the waiting time will have been wasted.

Which brings me to the justification for my project, [FAF Lobby Sim](https://github.com/EliotJones/FaFLobbySim). While the lobby is running on my desktop I don't want to have to sit checking it every few minutes. It would be good to be able to do other things elsewhere while having a way to check on the occupancy status.

### Design Log

What follows is the 'design' process I followed. As you can tell from reading it I really wasn't worrying too much about doing it right, just doing the bare minimum. Developing this way was fun, a nice break from proper open-source and job related development. I wanted to stay in C# .NET because it is the language I know best.

Whatever is stopping you starting on some project I hope this transparency helps encourage you to just churn out some code and get something shipped.

#### Getting started

So the lobby application is running on my computer and I want to check its status on my phone. The lobby for FAF involves 2 running processes. The Forged Alliance game application itself and the Java FAF client application.

The Java client consumes an API that can report the active lobbies and the occupancy state. This seems a natural starting point. If I can call the same APIs from a mobile client then I'll be done. There are 2 drawbacks to this though:

1. The API uses OAuth tokens for authentication. I'd need to get my own client id to call it. This is the sort of un-fun development work that involves coordinating between people and waiting for things to get done. Fine for work, not for a weekend project.
2. While the occupancy reported to the client by the API is generally correct it doesn't match the real state when open slots are manually marked 'closed' from within the game process. This can happen if a map has 12 slots but 2 of them would have insufficient resources. This is a rarity but combined with point 1 made me consider a different approach.

I hatched a Heath Robinson style plan. If I could run some client monitoring application on my desktop, pass the information to some server and then interact with that server from mobile I'd own all the hops. Should be easy right?

What's a plan without some arbitrary constraints? Since the client app will be calling a server I want to:

- Make the client app small, with minimal dependencies.
- Not pass anything other than the bare minimum information to the server.

#### Building the client app

So I've done some work with the FlaUI automation library in the past. Here's a [FizzBuzz implementation using the Windows 10 calculator](https://github.com/EliotJones/FizzBuzzCalc).

If I can use FlaUI and grab the information I need from the game lobby process it should be fairly simple. Just poll the game in a loop and report the information to the server. This will address the second drawback listed for using the API directly, since I'm reporting the actual game-state.

So I launch up [the FlaUI inspector tool](https://github.com/FlaUI/FlaUInspect) and... there's nothing there, just the window of the process. This is to be expected, it's a game after all, not a UI. The Java client process is far more amenable to UI automation, but it doesn't solve the real-state/closed slots problem I've now fixated on solving.

I vaguely recall a former boss once used OpenCV to 'bot' a simple game. I guess I can do something similar here. Since FlaUI can take screenshots of the open desktop it should be possible to parse out the info I need using image processing. I definitely don't want to send the screenshots to the server process since they will capture the entire desktop, not just the monitored process if other windows are moved in front of the monitored application.

#### Image processing

![lobby screenshot](/images/lobbysim/sample-lobby.png)

This screenshot shows the layout we need to parse. We 'just' need to pull out the number of "Open", "Closed" and occupied slots in the Nickname column.

Obviously we don't want to hardcode the positions since the game can run windowed or full-screen on different resolutions. The number of slots can vary from 2 - 16.

The obvious approach is to use OCR, e.g. Tesseract, to process the text and calculate occupancy from the Nickname column. Pulling in Tesseract means that you have a large dependency for the client application which isn't great if you want to redistribute it. Like everything it's much harder to get Tesseract working on Windows where this client app would be running.

I also played around with OpenCV and edge detection but didn't want the dependency or to read the documentation to figure out what I needed to do. So I settled on the questionable strategy of building the processing myself.

The first step is to threshold the image to simplify the input data. Playing around with the image in GIMP I found a reasonable threshold value:

![lobby screenshot with threshold applied](/images/lobbysim/sample-lobby-thresholded.png)

Applying a threshold to the screenshot was a simple one-liner with ImageSharp:

    image.Mutate(x => x.BinaryThreshold(0.39f));

I could then map the thresholded image to a C# `BitArray` to make it more memory efficient to work with. This would involve flattening the 2D image to an array so it was necessary to have a couple of methods to convert to/from 2D to 1D space:

    private static int XyToFlat(int x, int y, WidthHeight widthHeight)
            => (y * widthHeight.Width) + x;

    private static (int x, int y) FlatToXy(int index, WidthHeight widthHeight)
    {
        var y = index / (widthHeight.Width);
        var x = index - (y * widthHeight.Width);
        return (x, y);
    }

With these methods mapping a black-and-white image to a bit array is simple:

    private static BitArray FlattenThresholded(Image<Rgb24> image, WidthHeight wh)
    {
        var result = new BitArray(image.Height * image.Width);

        for (int row = 0; row < image.Height; row++)
        {
            for (int col = 0; col < image.Width; col++)
            {
                var flatIndex = XyToFlat(col, row, wh);
                if (image[col, row].R == 255)
                {
                    result[flatIndex] = true;
                }
            }
        }

        return result;
    }

So now we have our image in a black-and-white array (0 and 1). What next?

Well looking at the example thresholded image we can see that the region of interest is the table highlighted in orange:

<a href="/images/lobbysim/sample-lobby-thresholded-target-extracted.png" target="_blank">
![Table of interest in orange](/images/lobbysim/sample-lobby-thresholded-target-extracted.png)
</a>

If the slot is open there are 2 'words' in the Nickname column, "Open" and the chevron symbol for the dropdown containing the option to move into the slot or, if you are the host, close the slot. When I say 'word' here I mean nothing more group of pixels since this can be actual text, a faction symbol, a country flag, a block of color, or something else.

If the slot is closed there is only the word "Closed" and no chevron.

For slots occupied by a player there's either the player name and a chevron or, for the current user, simply the username.

Without OCR to distinguish between "Closed", "Open" and the current username you also need to use the "R" (Rank), "G" (Game count) or "Team" column to identify that a player occupies the current slot (the 'word' in the "Color" column may disappear due to thresholding).

The approach becomes:

1. Locate the table and columns.
2. If the Nickname column contains 1 word and no entries in other columns it is closed.
3. If the Nickname column contains 1 word and there are entries in other columns it is occupied by the current player (assuming a single word nickname)
4. If the Nickname contains 2 (or more) words and no entries in other columns it is open.
5. If the Nickname contains 2 (or more) words and one of the other columns (excluding "--") is occupied then the slot is occupied

To locate the table without hardcoding the position we need to first detect 'words'. As mentioned above these might be non-word groups of pixels but for our purposes since we're not running OCR the distinction is meaningless.

How do we detect words? Let's take a look at the "Nickname" text from the thresholded image:

![Nickname text](/images/lobbysim/scaled-nickname-thresholded.png)

If you're scanning the image top-to-bottom left-to-right when you first encounter a white pixel (marked in green in the image below) you can start the following process:

- Look around the neighboring pixels (ignoring pixels to the top-left since we scanned from that location) in a 9-by-9 region.
- Each time you find a white pixel in the neighborhood then move to that pixel (new green pixel), recording where you came from (marked blue in the image).
- Scan the neighborhood of the new pixel and move to any non-visited white pixels in that location (the new green pixel).

The first steps of this are shown below, the colors are purely illustrative:

![Initial flood fill steps](/images/lobbysim/scaled-nickname-step-2.png)

This is basically just a flood fill algorithm except you allow for gaps, both for diagonal pixels and entirely disjoint pixels like the dot on the 'i' and next letters in the word.

The problem with this approach is I wrote it recursively and it causes a stack overflow if the region to be filled is too large. Since large regions are just background noise in the thresholded image I just exit the recursion early if the region to be filled is too large, for some arbitrary value of too large. I should probably have implemented a proper flood fill algorithm instead.

Full code for this [is here](https://github.com/EliotJones/FaFLobbySim/blob/main/FaFLobbySim/FaFLobbySimClient/DetectImageWords.cs).

Using this flood fill word detection the resulting words are fairly well detected and defined:

![With word boundaries highlighted](/images/lobbysim/words-detected.png)

Once the 'words' are detected there are just some fairly [gnarly heuristics](https://github.com/EliotJones/FaFLobbySim/blob/main/FaFLobbySim/FaFLobbySimClient/CalculateLobbyOccupancy.cs) to group them into lines and identify the table header and columns.

### Building the server app

The client runs in a loop, every 5 seconds it uses FlaUI to take a screenshot of the lobby process, run word detection and use the heuristics discussed earlier to calculate the total slots and number occupied.

The server part is an incredibly simple MVC app that just holds the data in memory. The client posts to the [controller](https://github.com/EliotJones/FaFLobbySim/blob/main/FaFLobbySimServer/Controllers/UploadController.cs) which stores the latest occupancy and sends a server-sent event to any clients monitoring the lobby.

The idea is each lobby will be assigned a random identifier by the client on startup and multiple clients and lobbies may report occupancy to the server based on the identifier. In order just to ship it I hardcoded the lobby to a single identifier at this stage.

The final product is not great looking, but does work, and seems to update fairly reliably:

![The web UI showing 2 of 10 slots occupied](/images/lobbysim/lobby-monitor-site.png)

### Conclusion

Really at this point I couldn't be bothered to put in the necessary work to 'productionize' it. To make a real usable implementation I'd need to:

- Address the test case that is failing due to thresholding in ImageSharp not matching GIMP
- Make the heuristics for occupancy detection less flaky
- Package the client application and make it more reliable
- Improve the web UI and add rate limiting and other DDOS protection
- Dynamically generate lobby ids and assign a random token to each lobby id so only the client that first reports values for the lobby can send updates for it

With all those to-dos in mind I'm pleased I managed to get anything shipped and the few times I've used it it has meant I can get live updates pushed to my phone while being away from the computer.

The code for the client app is super funky and not at all idiomatic C#. I'm hoping to write a post about why I wrote it that way and what I learned from it soon.

#### Footnotes

1. An honorary mention to [Beyond All Reason](https://www.beyondallreason.info/) an open-source spiritual successor to Supreme Commander.
2. I don't believe this at all.
