# Alpha Release of PdfPig #

I'm very pleased to finally have reached the first alpha release of [PdfPig (NuGet)](https://www.nuget.org/packages/PdfPig/ "NuGet link").

<image src="https://raw.githubusercontent.com/UglyToad/Pdf/master/documentation/pdfpig.png" width="160px"/>

[PdfPig (GitHub)](https://github.com/UglyToad/PdfPig "GitHub link") is a library that reads text content from PDFs in C#. This will help users extract and index text from a PDF file using C#.

The current version of the library provides access to the text and text positions in PDF documents. 

### Motivation ###

The library began as an effort to port [PDFBox](https://pdfbox.apache.org/) from Java to C# in order to provide a native open-source solution for reading PDFs with C#. PdfPig is Apache 2.0 licensed and therefore avoids questionably (i.e. not at all) 'open-source' copyleft viral licenses.

I had been using the PDFBox library through [IKVM](https://www.codeproject.com/Articles/538617/Working-with-PDF-files-in-Csharp-using-PdfBox-and "CodeProject link to using PDFBox with IKVM") and started the project to investigate the effort required to make the PDFBox work natively with C#.

In order to understand the specification better I rewrote quite a few parts of the code resulting in many more bugs and fewer features than the original code.

As the alpha is (hopefully) used and issues are reported I will refine the initial public API. I can't forsee the API expanding much beyond its current surface area for the first proper release.

### Usage ###

To get the text from a PDF using C# with PdfPig the following code opens and retrieves a page from the document:

    using UglyToad.PdfPig;
    using UglyToad.PdfPig.Content;

    public static void Main()
    {
        using (PdfDocument document = PdfDocument.Open(@"C:\my-file.pdf"))
        {
            // Returns the total number of pages in the document.
            int pageCount = document.NumberOfPages;

            // Each page is loaded separately by this method.
            Page page = document.GetPage(1);

            // Sizes are given in typographic points (1/72nd of an inch).
            decimal widthInPoints = page.Width;
            decimal heightInPoints = page.Height;

            // The full text of the page as it is defined in the PDF content.
            string text = page.Text;

            // The text and location of each individual letter on the page.
            IReadOnlyList<Letter> letters = page.Letters;

            page = document.GetPage(2);

            // etc..
        }
    }

As far as possible the API is restricted to make discoverability easier and immutable to provide proper encapsulation.

Since PDF defines its content for display rather than readability purposes, the ```page.Text``` may lack spaces or present words in the wrong order.

For this reason it is necessary for the client to write their own code to form words from the letters provided by ```page.Letters```.

Please bear in mind this is the very first alpha release so highly likely to be unstable.

### Installation ###

The package is available from [NuGet](https://www.nuget.org/packages/PdfPig/). Since it is pre-release in order to see it you will need to tick the pre-release checkbox when searching.

From the Package Manager command line you can install it using:

    Install-Package PdfPig -Version 0.0.1-alpha-001