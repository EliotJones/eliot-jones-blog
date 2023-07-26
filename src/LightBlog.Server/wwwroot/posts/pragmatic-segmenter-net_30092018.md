# Sentence Boundary Detection in C# #

Sentence Boundary Detection or [Segmentation](https://en.wikipedia.org/wiki/Sentence_boundary_disambiguation) is the task of splitting an input passage of text into individual sentences. Since the period '.' character may be used in numbers, ellipses or names it's not enough to simply split by the period character.

When I was researching ways to do this in C# I didn't find much in the way of properly open source libraries. A lot of the libraries I found for other languages referred to the Golden Rule Set (GRS). This set comes from [Pragmatic Segmenter](https://github.com/diasks2/pragmatic_segmenter), a Ruby gem to segment text based on rules observed from a varied corpus of text.

Since I find porting code from other languages helps me understand both the variations in how different languages approach the same problems and also how other people make architectural decisions and structure their code I decided to port it to C#.

This Pragmatic Segmenter port is [available to download from NuGet](https://www.nuget.org/packages/PragmaticSegmenterNet/). The public API is similar to that for the Ruby package however the method is static:

    var result = Segmenter.Segment("There it is! I found it.");

    Assert.Equal(new[] { "There it is!", "I found it." }, result);

There is also support for other languages, the Language enum gives the supported languages:

    var result = Segmenter.Segment("Salve Sig.ra Mengoni! Come sta oggi?", Language.Italian);
    Assert.Equal(new[] { "Salve Sig.ra Mengoni!", "Come sta oggi?" }, result);


The [source code](https://github.com/UglyToad/PragmaticSegmenterNet) also contains a set of data from various sources I was using to test my port as well as add some behaviour for the sources I was primarily interested in (academic journals). This data can be [found here](https://github.com/UglyToad/PragmaticSegmenterNet/blob/master/PragmaticSegmenterNet.Tests.Unit/Languages/Data/EnglishTestData.xml). Hopefully this corpus of annotated sentence boundary data will be useful to people building their own libraries.