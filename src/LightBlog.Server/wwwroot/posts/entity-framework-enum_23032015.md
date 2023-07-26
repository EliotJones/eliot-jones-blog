# Entity Framework 6 Enums with String column#

I recently watched Jimmy Bogard's [excellent NDC presentation on Domain models][link0] to learn about how to implement a real world domain model.

One of the things Jimmy does in his presentation is to refactor his enum to use a base class allowing far more expressive enums with custom behaviour

There are a few versions of this custom enum base class around but I'm going to be using [this one][link1].

Annoyingly Entity Framework (EF) doesn't support mapping database fields to enums [unless you use an int column in your database][link2]. This might be good enough for your implementation however it seems a bit fragile to couple your database to a mystery int defined in code somewhere which could be changed by any user who doesn't realise what they're changing.

For a marginally less fragile and more domain model friendly enum I used an adapted version of Jimmy's enumeration class:

The main change is to the ```DisplayName``` property. We add a setter because Entity Framework is only going to bring back the string display name and we need to map to the underlying value field to get the proper enum.

    public string DisplayName
    {
        get
        {
            return this.displayName;
        }

        // Entity Framework will only retrieve and set the display name.
        // Use this setter to find the corresponding value as defined in the static fields.
        protected set
        {
            this.displayName = value;

            // Get the static fields on the inheriting type.
            foreach (var field in GetType().GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                // If the static field is an Enumeration type.
                var enumeration = field.GetValue(this) as Enumeration;
                if (enumeration == null)
                {
                    continue;
                }

                // Set the value of this instance to the value of the corresponding static type.
                if (string.Compare(enumeration.DisplayName, value, true) == 0)
                {
                    this.value = enumeration.Value;
                    break;
                }
            }
        }
    }

This simply checks the static fields on the instance of the enumeration class and finds the enumeration with the matching name.

We also change the private ```displayName``` and ```value``` fields to remove the readonly access modifier (and the underscores because I've never been able to adapt to the convention):

    private int value;
    private string displayName; 

I also overrode the ```==``` reference equals check to make our enumeration class behaves more like a value type:

    // Override reference equals to provide a value equals since the type is (almost) immutable.
    public static bool operator ==(Enumeration a, Enumeration b)
    {
        if (object.ReferenceEquals(a, b))
        {
            return true;
        }

        if ((object)a == null || (object)b == null)
        {
            return false;
        }

        var typeMatches = a.GetType().Equals(b.GetType());
        var valueMatches = a.Value.Equals(b.Value);

        return typeMatches && valueMatches;
    }

    public static bool operator !=(Enumeration a, Enumeration b)
    {
        return !(a == b);
    }

We need an additional step for our model configuration. Imagine we're storing a record of our prized Bonsai tree collection:

    public class BonsaiTree
    {
        public int Id { get; protected set; }

        public string Name { get; protected set; }

        public Genus Genus { get; protected set; }
    }

    public class Genus : Enumeration
    {
        public static readonly Genus Leafy = new Genus(0, "Leafy");

        public static readonly Genus Twiggy = new Genus(1, "Twiggy");

        private Genus() { }

        private Genus(int value, string displayName) : base(value, displayName) { }
    }

Where Bonsai tree is our EF entity. We need to tell EF that Genus is a complex type so it will map it to the same table. Additionally we need to ignore the value which we are not storing (if you needed to you could by extending this approach). Finally we need to configure the correct column name for the DisplayName of Genus:

    public class EnumTestContext : DbContext
    {
        public DbSet<BonsaiTree> BonsaiTrees { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ComplexType<Genus>().Ignore(bt => bt.Value);
            modelBuilder.Types<BonsaiTree>().Configure(t => t.Property(bt => bt.Genus.DisplayName).HasColumnName("Genus"));
        }
    }

I've only used this approach with Code First against an existing database so I'm not sure the migrations would handle it properly and I haven't tested it extensively however in a quick test it seemed to work well. Also the more derived Enum types for proper logic separation as detailed in the [end of this post][link1] won't work because of how EF handles (or rather doesn't handle) inheritance in its mapping.

The full code is [here][link3]; sorry about half the namespace disappearing, I was trying desperately to get PHP to stop displaying the Byte Order Mark and instead succeeded in getting it to eat the code.

[link0]: https://vimeo.com/43598193 "Jimmy Bogard - Crafting Wicked Domain Models at Norwegian Developers Conference"
[link1]: https://lostechies.com/jimmybogard/2008/08/12/enumeration-classes/ "Jimmy Bogard - Custom Enumeration Class"
[link2]: https://msdn.microsoft.com/en-gb/data/hh859576.aspx "Enum support in EF5+"
[link3]: http://eliot-jones.com/Code/ef-enum/EnumTest.cs "Code file for samples in this post"