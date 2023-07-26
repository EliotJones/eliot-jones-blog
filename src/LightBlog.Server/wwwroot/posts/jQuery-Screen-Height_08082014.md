#jQuery Fit Footer To Window Height

Sometimes your content just doesn't fit the window height, for exampling when you're styling a page with placeholder content. Or, for a short post like this. 

That's why I use the script below for making sure my footer appears at the bottom of pages where the content is shorter than the window:
	
	function AdjustWindowHeight()
	{
	    var requiredHeight = $(window).height() - $("#footer").height();
	    $('#wrap').css('min-height', requiredHeight);
	}
	
	$(window).on("resize", AdjustWindowHeight);
	
	$(function () {
	    AdjustWindowHeight();
	});

Or the minified version:

    function F(){var a=$(window).height()-$("#footer").height(); $("#wrap").css("min-height",a)}$(window).on("resize",F);$(function(){F()});

All you need is a div containing all your footer content and and wrap div containing all other content:

    <body>
    <div id="wrap">Content</div>
    <div id="footer">Footer :)</div>
    </body>