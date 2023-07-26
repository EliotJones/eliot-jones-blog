# Open and Create PNG Images in C# #

I wanted a platform independent way to open and create PNG images in C#. [BigGustave](https://github.com/EliotJones/BigGustave) is a new library which provides a .NET Standard 2.0 compatible way of opening and creating PNG images.

To open a png image you can pass either the bytes or the stream of the image to `Png.Open` and then retrieve the values for pixels at any location in the image:

    Png png = Png.Open(File.ReadAllBytes(@"C:\pictures\example.png"));
    Pixel first = png.GetPixel(0, 0);
    Console.WriteLine($"R: {first.R}, G: {first.G}, B: {first.B}");

To create a .png image in C# use the `PngBuilder` to define pixel values before saving to an output stream:

    var builder = PngBuilder.Create(2, 2, false);

    var red = new Pixel(255, 0, 0);

    builder.SetPixel(red, 0, 0);
    builder.SetPixel(red, 1, 1);

    using (var memory = new MemoryStream())
    {
        builder.Save(memory);
        
        return memory.ToArray();
    }

BigGustave is completely open source and is [available on NuGet](https://www.nuget.org/packages/BigGustave/) now so if you need very basic PNG manipulation tools for platform independent .NET code why not check it out?