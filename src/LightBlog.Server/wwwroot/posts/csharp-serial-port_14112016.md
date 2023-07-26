# Reading from a COM port #

I was recently researching how to read from a COM (Serial) port in order to communicate with an old embedded system. If you're not familiar with them that's one of these:

![a serial port has a 9 pin connection][img0]

C# provides the ```SerialPort``` class for just this purpose which is hugely helpful. However there's one problem with this class which is that it's just not... very good. I didn't know about this but [this post][link0] by Ben Voigt should leave you in no doubt that it has some problems.

### Setup ###

In order to test the code on my machine without access to the actual device which was elsewhere, I installed Eltima's [Virtual Serial Port Driver][link1] which would allow me to test with emulated serial ports. Once I had set up a pair of ports I could send data from one end using PowerShell. 

I created a pair of virtual ports called COM1 and COM2, COM1 would be the port I was using C# to read from. COM2 would emulate the device sending data.

For testing I was just sending blocks of text from COM2 using the following PowerShell script:

    $port = New-Object System.IO.Ports.SerialPort COM2,9600,None,8,one
    $port.Open()
    $port.WriteLine("some test data")
    
### The Code ###
    
Because the application I was using to read from the port would be a C# console application I didn't have any way to use async-await properly so stuck with the approach outline in [Ben's blog post][link0], namely using ```IAsyncResult```.

The code which listens to and reads from the result looks like this:

    public class COMReader
    {
        private static readonly BlockingCollection<byte[]> queue = new BlockingCollection<byte[]>();

        public static int Main()
        {
            using (var port = new SerialPort("COM1", 9600, Parity.None, 8, StopBits.One)
            {
                Handshake = Handshake.None,
                ReadTimeout = 1000,
                WriteTimeout = 1000
            })
            {
                port.Open();
                
                var consumer = new Consumer(queue);
                var buffer = new byte[256];
                Action startListen = null;
                var onResult = new AsyncCallback(result => OnResult(result, startListen, port, buffer));

                Task.Run(() => consumer.Start());
                
                startListen = () => 
                {
                    port.BaseStream.BeginRead(buffer, 0, buffer.Length, onResult, null);
                };

                startListen();

                while (true && port.IsOpen)
                {
                    // handle user's console window interaction.
                }

                queue.CompleteAdding();

                if (port.IsOpen)
                {
                    port.Close();
                }
            }

            return 0;
        }

        private static void OnResult(IAsyncResult result, Action startListen, SerialPort port, byte[] buffer)
        {
            try
            {
                if (!port.IsOpen)
                {
                    return;
                }

                var actualLength = port.BaseStream.EndRead(result);

                var received = new byte[actualLength];

                Buffer.BlockCopy(buffer, 0, received, 0, actualLength);

                queue.Add(received);
            }
            catch (IOException)
            {
                Console.WriteLine("I/O exception encountered. Closing.");
                port.Close();
                queue.CompleteAdding();
                return;
            }

            startListen();
        }
    }

Most of this is fairly self explanatory, we create a ```SerialPort``` instance (in a ```using``` statement since it implements ```IDisposable```) and then open it. We also create a new ```Consumer```, which I will talk about in a moment.

Next as in the blog post I mentioned we have an action to execute, this is declared separately to its assignment since it needs to run itself when it finishes reading. The body of the code to execute once the async read finishes is in the ```OnResult``` method. This just reads the current bytes from the port's stream.

The next step is to start the ```Consumer``` running on another thread using ```Task.Run()```. Then we simply start the action which listens to the port.

This will listen for incoming messages on the port and each time something is received it will call the code in ```OnResult(..)``` and then start listening again.

### Consuming ###

In order to read the output data two things have to be done:

+ Return to the ```startListen()``` method at the end of ```OnResult``` as quickly as possible to avoid missing data.
+ Notify some subscriber to the data and work out if the received data forms a complete message.

Since the SerialPort will read data whenever it feels like it instead of waiting for a complete message we need to know what our definition of a "message" is, in the case of my PowerShell script its end is the ```\n``` character.

This is an almost perfect use case for a ```BlockingCollection```, this provides a location for the ```OnResult``` method to dump the data and allows us to write a consumer for the data which only runs when new data is found.

Our ```Consumer``` class is constructed by passing in the blocking collection ```queue``` which the main method writes to:

    var consumer = new Consumer(queue);

The code for consumer is below:

    public class Consumer
    {
        private const byte TerminatingCharacter = 10;

        private readonly BlockingCollection<byte[]> producer;

        private readonly List<byte> bytes = new List<byte>();

        public Consumer(BlockingCollection<byte[]> producer)
        {
            this.producer = producer;
        }

        public void Start()
        {
            Console.WriteLine("Start listening");

            foreach (var item in producer.GetConsumingEnumerable())
            {
                ProcessInput(item);
            }

            Console.WriteLine("Finish listening");
        }

        private void ProcessInput(byte[] input)
        {
            if (input == null || input.Length == 0)
            {
                return;
            }

            if (input[input.Length - 1] == TerminatingCharacter)
            {
                var message = Encoding.UTF8.GetString(bytes.Concat(input).ToArray());

                Console.WriteLine($"Message Received: {Environment.NewLine}{message}{Environment.NewLine}");

                bytes.Clear();
            }
            else
            {
                bytes.AddRange(input);
            }
        }
    }
    
This class keeps a reference to the producer's blocking collection. When it starts listening using the ```Start``` method it will keep listening to the collection until ```queue.CompleteAdding();``` is called. Every time a new item is added to the queue the consumer will run the ```ProcessInput``` method where ```item``` is the byte array we placed on the queue in the producer.

Using this approach we can perform a slower analysis of the messages we have received so far in order to pull the complete messages out. For this demo application I just check for the last byte received to be the character ```\n``` but your definition of message may be more complex depending on the device which produces the message.

Because we run the consumer on its own thread:

    Task.Run(() => consumer.Start());
    
The time taken for the listener to respond to new data from the port is completely decoupled from the code to process these events.

This isn't perfect code because in the failure case it doesn't wait for the consumer to finish processing the queue before shutting down and there is insufficient error handling around parts of it. However it will hopefully provide an idea how the ```BlockingCollection``` can be used to communicate between a publisher and subscriber on separate threads and how that principle can be applied to Serial Ports.

### 4096 limit ###

It's worth noting that by default a message is limited to 4096 bytes, if the end of the message isn't received in this time the rest of the message will be dropped. In order to avoid this set the ```ReadBufferSize``` on the ```SerialPort``` to something higher than 4096.

I'm not an embedded systems person so all of this is my best guess, if I've done something completely idiotic, please let me know in the comments.

[link0]: http://www.sparxeng.com/blog/software/must-use-net-system-io-ports-serialport
[link1]: http://www.eltima.com/products/vspdxp/
[img0]: https://upload.wikimedia.org/wikipedia/commons/e/ea/Serial_port.jpg