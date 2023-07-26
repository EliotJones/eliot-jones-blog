# Run tests task for Elixir in Visual Studio Code #

In order to learn Linux I'm biting off more than I can chew by trying to learn [Elixir][elixir] at the same time. From the available code editors for Linux I'm using [Visual Studio Code][vscode].

Visual Studio Code has the ability to configure custom tasks.

To get to the custom task configuration you can use the command palette with ```Ctrl + Shift + P``` and type ```Tasks: Configure Task Runner```. This will open "tasks.json". Replace the contents with:

    {
        "version": "0.1.0",
        "command": "bash",
        "isShellCommand": true,
        "showOutput": "always",
        "args": [
            "-c"
        ],
        "tasks": [
            {
                "taskName": "Elixir-Test",
                "suppressTaskName": true,
                "isTestCommand": true,
                "args": ["mix test"]
            }
        ]
    }

And save the file. Now if you use ```Ctrl + Shift + T``` mix will run your tests for the project (assuming you have the ```mix.exs``` level folder open) and print the output to the console.

This command could probably be improved by checking that it's executing in the correct folder first but I'm nowhere near confident enough with Bash to write it yet.

### Further notes from adventures in Elixir ###

#### Function capturing (Capture operator) ####

This concept took me a little while to get to grips with because I couldn't think of an equivalent from C#.

A typical anonymous function in Elixir might take this form:

    square = fn x -> x * x end

You would then call it with the dot "." between the function name and the argument like this:

    # Outputs 9
    square.(3)

The **function capturing operator** (&) allows us to express the same function even more quickly:

    square = &(&1 * &1)

We call this function in the same way and the output is exactly the same:

    # Outputs 9
    square.(3)

You could break this down further to understand it:

<pre><code class=" hljs cs">
# Start anonymous function definition with unknown number of arguments.
# e.g. fn ? ->
&(

# Take the first argument (&1) to the anonymous function and multiply by the same argument
&1 * &1

# End anonymous function
)
</code></pre>

If you try to pass more than one argument to this function you will get an error:

<pre><code class=" hljs cs">
# (BadArityError) function with arity 1 called with 2 arguments (0, 3)
square.(0, 3)
</code></pre>

The arity is the number of arguments the function takes. 

Equally if you try to define the function without using all the positional arguments in the function you get a compile error:

    # Won't compile
    square = &(&2 * &2)

There is another form of function capture which only works with named functions. For example for the standard anonymous function (using the length named function from the String module):

    len = fn s -> String.length(s) end

Running that will give the following output:

    # Outputs 9
    len.("chow chow")

Using our current understanding of the capture operator we could rewrite to the equivalent but shorter form:

    len = &(String.length(&1))

There is a second form we can use which is equivalent:

    len2 = &String.length/1    

We can also verify these functions are identical:

    # Outputs true
    len == len2

[elixir]: http://elixir-lang.org/ "Official Elixir language site"
[vscode]: https://code.visualstudio.com/ "Visual Studio Code site"