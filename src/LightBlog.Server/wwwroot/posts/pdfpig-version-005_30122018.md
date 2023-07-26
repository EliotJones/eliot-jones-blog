# PdfPig Version 0.0.5 #

Today [version 0.0.5 of PdfPig was released](https://www.nuget.org/packages/PdfPig/). This is the first version which includes the ability to create PDF documents in C#.

There aren't many fully open source options around for both reading and writing PDF documents so the addition of PDF document creation to PdfPig is an exciting next step for the API.

The actual design of document creation isn't finished yet and there's more work to be done around the currently unsupported use cases such as splitting, merging and editing existing documents as well as adding non-ASCII text, working with forms and adding images to new documents but the functionality in 0.0.5 should provide enough for simple use cases and the open source Apache 2.0 license means that it can be used in commercial software.

You can create a new document using a document builder:

    PdfDocumentBuilder builder = new PdfDocumentBuilder();

This creates a completely empty document. To add the first page we use the imaginatively named add page method.

    PdfPageBuilder page = builder.AddPage(PageSize.A4);

This supports various page sizes defined by the ```PageSize``` enum, such as the North American standard ```PageSize.Letter```. It also allows the choice of portrait (default) or landscape pages.

Once a page builder has been created text, lines and rectangles can be added to it. 

### Fonts ###

In order to draw text a font must be chosen. Version 0.0.5 supports TrueType fonts as well as the 14 default fonts detailed in the PDF Specification. These are called the Standard 14 fonts and while their use is beginning to be phased out, all PDF readers should still support them.

A Standard 14 font is the quickest way to get started since it doesn't require a TrueType file. Because the fonts are already well defined and included with PDF reading software the font does not need to be embedded in the PDF file, leading to much smaller file sizes. The available Standard 14 fonts are represented by the ```Standard14Font``` enum. They are different flavours of the following fonts:

+ Times New Roman
+ Helvetica
+ Courier
+ Symbol
+ Zapf Dingbats

To use a font it should be registered with the document builder. By adding the font to the document builder rather than at the page level, it can be shared across pages in multi-page documents. To use Helvetica:

    PdfDocumentBuilder.AddedFont font = builder.AddStandard14Font(Standard14Font.Helvetica);

Or for Times New Roman:

    PdfDocumentBuilder.AddedFont font = builder.AddStandard14Font(Standard14Font.TimesRoman);

### Text ###

Once a font is registered with a document builder, all pages in that document can use it to draw text. To write some text on the page created earlier we call the ```AddText``` method:


    IReadOnlyList<Letter> letters = page.AddText("Hello World!", 12, new PdfPoint(25, 520), font);

This call will write the text "Hello World" in size 12 font at the provided location, 25 units from the left edge of the document and 520 units from the bottom of the document. Remember coordinates in PDF run up from 0 at the bottom rather than having 0 at the top as in WPF or other graphics systems. The last parameter is the font we registered earlier.

This method adds the necessary operations to the PDF content stream for this page and returns the letters that will be drawn on the page and their position. This allows you to measure text and draw underlines, or borders around it.

If you want to just find out the sizes and positions of the letters prior to drawing, for example to calculate text wrapping, you can use the measure method:

    IReadOnlyList<Letter> letters = page.MeasureText("Hello World!", 12, new PdfPoint(25, 520), font);

The only difference with this method is that it will not add the text to the page and so does not appear in the output document.

### Geometry ###

In addition to text you can also draw lines and rectangles:

    page.DrawLine(new PdfPoint(25, 70), new PdfPoint(100, 70), 3);
    page.DrawRectangle(new PdfPoint(30, 200), 250, 100, 0.5m);

This draws a line from (25, 70) to (100, 70) with a line width of 3 units. This means a horizontal line is drawn 70 units from the bottom of the page from 25 units from the left edge to 100 units from the left edge.

The rectangle will be drawn from (30, 200) with a width of 250 units and a height of 100 units with a line width of 0.5 units.

### Color ###

The color of text and geometry can also be set. Stroke color (the color of lines) and Fill/Text color are set by separate methods:

    // Set line color in RGB.
    page.SetStrokeColor(250, 132, 131);
    // Set text color in RGB (and the color of any fill operations, not currently used).
    page.SetTextAndFillColor(250, 132, 131);

These colors are expressed in RGB from 0 - 255 for the red, green and blue component. Internally PDF uses values from 0 - 1.0 for RGB, this conversion happens within the page builder.

Once a color is set it will remain active for all future calls to add text, drawing lines and drawing rectangles. To set the color back to the default black you must call:

    page.ResetColor();

Which means further calls will use the color black.

### Full Example ###

The simplest "Hello World!" example is:

    PdfDocumentBuilder builder = new PdfDocumentBuilder();

    PdfPageBuilder page = builder.AddPage(PageSize.A4);

    PdfDocumentBuilder.AddedFont font = builder.AddStandard14Font(Standard14Font.Helvetica);

    page.AddText("Hello World!", 12, new PdfPoint(25, 520), font);

    byte[] result = builder.Build();

Further examples including multiple pages can be found in the [tests for the document builder](https://github.com/UglyToad/PdfPig/blob/master/src/UglyToad.PdfPig.Tests/Writer/PdfDocumentBuilderTests.cs).

### TrueType ###

In addition to Standard 14 fonts TrueType fonts (fonts with the .ttf extension) are also supported. The raw bytes of the font files must be provided to the ```AddTrueTypeFont``` method. This example uses the System font Baskerville Old Face on Windows 10:

    string file = @"C:\Windows\Fonts\BASKVILL.TTF";

    byte[] bytes = File.ReadAllBytes(file);
    
    PdfDocumentBuilder.AddedFont font = builder.AddTrueTypeFont(bytes);

Since the result of adding a TrueType font is an ```PdfDocumentBuilder.AddedFont``` which is identical to the result for Standard 14 fonts it can be used in the same way to add and measure text for pages.

The TrueType font is interpreted and a compressed version of the font file is embedded and distributed with the resulting PDF document. Currently there are no optimizations to use a subset of the font file which most other document producers do so the file size for documents will be larger.

### Conclusion ###

While it's still early days for the library the addition of Open Source C# PDF document creation should widen the appeal of the [PdfPig](https://github.com/UglyToad/PdfPig) library and help people who need a truly open source, fully managed .NET PDF creator.
