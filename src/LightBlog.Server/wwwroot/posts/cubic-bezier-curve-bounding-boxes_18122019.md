# Bezier Curve Bounding Boxes #

One of the challenges for generating accurate character sizes in a PDF document I encountered while building [PdfPig](https://github.com/UglyToad/PdfPig) was working out the bounding box for a cubic Bezier curve.

A [cubic Bezier curve](https://en.wikipedia.org/wiki/B%C3%A9zier_curve#Cubic_B%C3%A9zier_curves) is defined by 4 points; the start, end and 2 control points.

We can number the points for use in the formulae in this post:

+ Start: P0
+ Control 1: P1
+ Control 2: P2
+ End: P3

This gives the formula for the Bezier curve:

![formula from Wikipedia](https://wikimedia.org/api/rest_v1/media/math/render/svg/504c44ca5c5f1da2b6cb1702ad9d1afa27cc1ee0)

We can take advantage of the property that the gradient of the curve will be 0 at the bounds of the curve (as well as including the start and end of the curve).

To find where the gradient is 0 we can differentiate the curve and then solve the differentiated equation for 0.

The formula differentiated (from Wikipedia) is:

![formula from Wikipedia](https://wikimedia.org/api/rest_v1/media/math/render/svg/bda9197c2e77c17d90839b951cb0035d79c8d417)

Where `P0` is the start control point with both x and y values. Taking just the X dimension (and moving the `(1 - t)`), we rewrite the formula as:

    P'(x) = 3(P1.x - P0.x)(1 - t)^2 + 6(P2.x - P1.x)(1 - t)t + 3(P3.x - P2.x)t^2

Next we can assign some names to some known values:

    var i = (P1.x - P0.x)
    var j = (P2.x - P1.x)
    var k = (P3.x - P2.x)

Allowing us to rewrite the formula as:

    P'(x) = 3i(1 - t)^2 + 6j(1 - t)t + 3kt^2

Now let's multiply out the full thing:

    P'(x) = 3i - 3it - 3it + 3it^2 + 6jt - 6jt^2 + 3kt^2

And tidy it up a bit:

    P'(x) = 3i - 6it + 6jt + 3it^2 - 6jt^2 + 3kt^2
    P'(x) = 3i + (-6i + 6j)t + (3i - 6j + 3k)t^2

Given we are finding the point where the gradient is 0:

    3i + (-6i + 6j)t + (3i - 6j + 3k)t^2 = 0

If we reorder the terms we see this is a quadratic equation in disguise:

    (3i - 6j + 3k)t^2 + (-6i + 6j)t + 3i = 0

The quadratic equation given by:

    ax^2 + bx + c = 0

Means we need the following values:

    a = (3i - 6j + 3k);
    b = (6j - 6i);
    c = 3i;

This is just a quadratic equation which we can solve using the quadratic formula to find the value of t:

    t = -b (+/-) sqrt(b^2 - 4ac)) / 2a

We will get between 0 and 2 solutions for `t` depending on if:

    b^2 - 4ac

Is greater than zero. In addition the start and end control points provide us with the other points to build our bounding box.

How might this look in code? My [full implementation is here](https://github.com/UglyToad/PdfPig/blob/master/src/UglyToad.PdfPig/Geometry/PdfPath.cs#L521) which is used to calculate the bounding box of Bezier curves in PDF documents and Type 1 fonts.

To start with let's define our points:

    Point start = new Point(5, 7);
    Point end = new Point(10, 9);
    Point control1 = new Point(6, 5);
    Point control2 = new Point(9, 12);

Now taking just the x dimension and the naming we established above:

    double p0 = start.X;
    double p1 = control1.X;
    double p2 = control2.X;
    double p3 = end.X;

Now we can substitute our named values (i, j and k):

    double i = p1 - p0;
    double j = p2 - p1;
    double k = p3 - p2;

Now for the parts of the quadratic equation (a, b and c) we established above:

    double a = (3i - 6j + 3k);
    double b = (6j - 6i);
    double c = 3i;

First we need to check if there are any real numbers that solve the `sqrt(b^2 - 4ac)` part of the quadratic equation. If the result of `b^2 - 4ac` is negative there are only imaginary solutions to the problem.

    double sqrtPart = (b * b) - (4 * a * c);
    bool hasSolution = sqrtPart >= 0;
    if (!hasSolution) return;

If we have real solutions to the square root we can then evaluate the solutions for the value of t:

    double t1 = (-b + Math.Sqrt(sqrtPart)) / (2 * a);
    double t2 = (-b - Math.Sqrt(sqrtPart)) / (2 * a);

For Bezier curves, values of `t` are only valid if they lie between 0 and 1, so if we have values in this range we can substitute them into the original Bezier equation to find the minimum and maximum values of x:

    P = (1 - t)^3(P0) + 3(1 - t)^2(t)(P1) 
        + 3(1 - t)(t^2)(P2) + (t^3)P3

So in code:

    public static double GetSolutionForT(double t, double p0, double p1, double p2, double p3)
    {
        double oneMinusT = (1 - t);

        return (Math.Pow(oneMinusT, 3) * p0)
            + (3 * Math.Pow(oneMinusT, 2) * t * p1)
            + (3 * oneMinusT * Math.Pow(t, 2) * p2)
            + (Math.Pow(t, 3) * p3);
    }

This will give 0, 1 or 2 solutions for t (depending on whether t0 and t1 were in the range 0 to 1). The same equation can then be run for y by simply setting `p0 = start.Y;` and the same for other points.

The full code is:

    public struct Point
    {
        public double X { get; }
        public double Y { get; }

        public Point(double x, double y)
        {
            X = x;
            Y = y;
        }
    }

    public struct Rectangle
    {
        public Point Min { get; }
        public Point Max { get; }

        public Rectangle(Point min, Point max)
        {
            Min = min;
            Max = max;
        }
    }

    public static class BezierSolution
    {
        public static Rectangle GetBounds(Point start, Point control1, Point control2, Point end)
        {
            (double? solX1, double? solX2) = SolveQuadratic(start.X, control1.X, control2.X, end.X);
            (double? solY1, double? solY2) = SolveQuadratic(start.Y, control1.Y, control2.Y, end.Y);

            var minX = Math.Min(start.X, end.X);
            var maxX = Math.Max(start.X, end.X);

            if (solX1.HasValue)
            {
                minX = Math.Min(minX, solX1.Value);
                maxX = Math.Max(maxX, solX1.Value);
            }

            if (solX2.HasValue)
            {
                minX = Math.Min(minX, solX2.Value);
                maxX = Math.Max(maxX, solX2.Value);
            }
                
            var minY = Math.Min(start.Y, end.Y);
            var maxY = Math.Max(start.Y, end.Y);

            if (solY1.HasValue)
            {
                minY = Math.Min(minY, solY1.Value);
                maxY = Math.Max(maxY, solY1.Value);
            }

            if (solY2.HasValue)
            {
                minY = Math.Min(minY, solY2.Value);
                maxY = Math.Max(maxY, solY2.Value);
            }

            return new Rectangle(new Point(minX, minY), new Point(maxX, maxY));
        }

        private static (double? solution1, double? solution2) SolveQuadratic(double p0, double p1, double p2, double p3)
        {
            double i = p1 - p0;
            double j = p2 - p1;
            double k = p3 - p2;

            // P'(x) = (3i - 6j + 3k)t^2 + (-6i + 6j)t + 3i
            double a = (3 * i) - (6 * j) + (3 * k);
            double b = (6 * j) - (6 * i);
            double c = (3 * i);

            double sqrtPart = (b * b) - (4 * a * c);
            bool hasSolution = sqrtPart >= 0;
            if (!hasSolution)
            {
                return (null, null);
            }

            double t1 = (-b + Math.Sqrt(sqrtPart)) / (2 * a);
            double t2 = (-b - Math.Sqrt(sqrtPart)) / (2 * a);

            double? s1 = null;
            double? s2 = null;

            if (t1 >= 0 && t1 <= 1)
            {
                s1 = GetBezierValueForT(t1, p0, p1, p2, p3);
            }

            if (t2 >= 0 && t2 <= 1)
            {
                s2 = GetBezierValueForT(t2, p0, p1, p2, p3);
            }

            return (s1, s2);
        }

        private static double GetBezierValueForT(double t, double p0, double p1, double p2, double p3)
        {
            double oneMinusT = 1 - t;

            return (Math.Pow(oneMinusT, 3) * p0)
                    + (3 * Math.Pow(oneMinusT, 2) * t * p1)
                    + (3 * oneMinusT * Math.Pow(t, 2) * p2)
                    + (Math.Pow(t, 3) * p3);
        }
    }