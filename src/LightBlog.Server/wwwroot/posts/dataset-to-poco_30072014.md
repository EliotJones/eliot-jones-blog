#POCOs from DataTable

*Edit: There were a few code errors in the original post, these have now been fixed.*

For those of us still using Stored Procedures to retrieve information from a database it's useful to have a quick way to pass the resulting DataSet to a collection of POCOs (Plain Old CLR Objects).

<img src = "/images/pocos/DataSet.png" alt = "A dataset in Visual Studio debugger"/>

The problem is manual mappings are a pain and if they're spread around your data access logic lead to a maintenance headache. That's why **[this approach][link0]** by **Ricardo Rodrigues** is so appealing. As soon as I came across it I decided to use it for all future data access logic on the application I maintain. 

The database for this application is a sprawling nightmare of non-existent constraints, mixed naming conventions and bad data-type choices so his approach was a natural fit.

### Original Method

To outline the approach (this is mostly a copy of what's on his site but with a few changes relevant to this post), you use a ColumnMapping attribute:

	public class ColumnMapping : Attribute
    {
        public string FieldName { get; set; }
    }

This attribute is then placed above the properties on a POCO to map the column names of the dataset to the property:

	public class Dog
    {
        [ColumnMapping(FieldName="Dog_Id")]
        public int Id { get; set; }

        [ColumnMapping(FieldName = "NumLegs")]
        public int NumberOfLegs { get; set; }

        [ColumnMapping(FieldName = "Description")]
        public string Name { get; set; }

        [ColumnMapping(FieldName = "Owner")]
        public bool HasOwner { get; set; }
    }

This allows you to compensate for poor life choices on the part of the original db developer by having your POCO properties nicely named.

The mapping is then performed as follows:

	public class DbUtil
    {
        public List<T> ToObjectList<T>(DataTable dataTable) where T : new()
        {
            List<T> returnList = new List<T>();
            for (int rowIndex = 0; rowIndex < dataTable.Rows.Count; rowIndex++)
            {
                T returnObject = new T();

                for (int colIndex = 0; colIndex < dataTable.Columns.Count; colIndex++)
                {
                    DataColumn column = dataTable.Columns[colIndex];

                    foreach (PropertyInfo property in returnObject.GetType().GetProperties())
                    {
                        String fieldName = ((ColumnMapping)property.GetCustomAttributes(
												typeof(ColumnMapping), false)[0]).FieldName;
                        if (fieldName.ToLower() == column.ColumnName.ToLower())
                        {
                            property.SetValue(returnObject,
                                FieldToObject(dataTable.Rows[rowIndex][colIndex], property.PropertyType),
                                null);
                            break;
                        }
                    }
                }
                returnList.Add(returnObject);
            }
            return returnList;
        }
    }

The FieldToObject method is just a quick and nasty method I chucked together for this demo. 

	private object FieldToObject(object obj, Type type)
    {
        if (type == typeof(int))
        {
            int output = 0;
            int.TryParse(obj.ToString(), out output);
            return output;
        }
        else if (type == typeof(bool))
        {
            try
            {
                return Convert.ToBoolean(obj);
            }
            catch { return false; }
        }
        else
        {
            return obj;
        }
    }
I use something similar for the application, it allows you to sanitise your data as you map, for instance some fields which should have a data type of Bit for boolean such as "IsValid" actually have int.  
This means you're trying to cast values -1, 0, 1, 2, 3, etc to bool and you need a way to specify the expected behaviour in these cases.

### Fine Tuning

Having thought some more about this recently it seems like there's room for performance improvements. So I've put together a thoroughly unscientific test. The DataTable is seeded as follows, trying to match the original database's randomness:

	DataTable dataTable = new DataTable("DogsDataTable");
    dataTable.Columns.Add("Dog_Id");
    dataTable.Columns.Add("NumLegs");
    dataTable.Columns.Add("Description");
    dataTable.Columns.Add("Owner");

    Random rnd = new Random();
    for (int i = 0; i < 1000; i++)
    {
        DataRow tempRow = dataTable.NewRow();

        bool? owner = true;
        if(i % 3 == 0) owner = false;
        else if (i % 11 == 0) owner = null;

        tempRow["Dog_Id"] = i + 1;
        tempRow["NumLegs"] = rnd.Next(1, 5);
        tempRow["Description"] = "Dog" + i.ToString();
        tempRow["Owner"] = owner;

        dataTable.Rows.Add(tempRow);
    }

The DataTable is then parsed using a variety of methods and the time for 500 passes recorded using [Stopwatch][link1] to measure C# runtime as follows:

	DbUtil dbUtil = new DbUtil();
    long elapsedMilliseconds = 0;
    int passes = 500;
    for (int t = 0; t < passes; t++)
    {
        Stopwatch stopWatch = Stopwatch.StartNew();
        List<Dog> dogs = dbUtil.ToObjectList<Dog>(dataTable); // This method is swapped.
        stopWatch.Stop();
        elapsedMilliseconds += stopWatch.ElapsedMilliseconds;
    }
    Console.WriteLine("average milliseconds = " + (elapsedMilliseconds / passes).ToString() 
        + " passes = " + passes.ToString()
        + " total milliseconds = " + elapsedMilliseconds.ToString());

(Note the program is run rather than debugged, debugging adds an extra 320 ms roughly to the runtime)   
For Ricardo's method the average time is: **46 ms**

Here is a manual mapping, using my same rubbishy casting method to keep it fair.
	
	public List<Dog> ToDogList(DataTable dataTable)
    {
        List<Dog> returnList = new List<Dog>();

        for (int rowIndex = 0; rowIndex < dataTable.Rows.Count; rowIndex++)
        {
            returnList.Add(new Dog
            {
                Id = (int)FieldToObject(dataTable.Rows[rowIndex]["Dog_Id"], typeof(int)),
                Name = (string)FieldToObject(dataTable.Rows[rowIndex]["Description"], typeof(string)),
                NumberOfLegs = (int)FieldToObject(dataTable.Rows[rowIndex]["NumLegs"], typeof(int)),
                HasOwner = (bool)FieldToObject(dataTable.Rows[rowIndex]["Owner"], typeof(bool))
            });
        }

        return returnList;
    }
This method runs in: **3 ms**

Now we take Ricardo's method and try to take advantage of manual mapping using the [Contains method][link2] to find if a column exists in the DataTable:

	public List<T> ToObjectList2<T>(DataTable dataTable) where T : new()
    {
	    List<T> returnList = new List<T>();
	    for (int rowIndex = 0; rowIndex < dataTable.Rows.Count; rowIndex++)
	    {
	        T returnObject = new T();
	        foreach (PropertyInfo property in returnObject.GetType().GetProperties())
	        {
	            string fieldName = ((ColumnMapping)property.GetCustomAttributes(typeof(ColumnMapping), false)[0]).FieldName;
	            if(dataTable.Columns.Contains(fieldName))
	            {
	                property.SetValue(returnObject,
	                    FieldToObject(dataTable.Rows[rowIndex][fieldName], property.PropertyType),
	                    null);
	            }
	        }
	        returnList.Add(returnObject);
	    }
	    return returnList;
	}

This runs in: **24 ms**

That's pretty good but is it possible to squeeze more performance out?

There's a slight performance benefit (~500 ticks) to declaring the capacity of the object list up-front (as expected due to not needing to grow the list). This is a surprisingly small benefit and shows just how well C# handles list lengths changing.

### Final Result

The run-time can be reduced to **4 ms** by making the changes below:

A new class is declared inside DbUtil:

	private class ExtendedPropertyInfo
    {
        public PropertyInfo property { get; set; }
        public string fieldName { get; set; }
    }

The method then looks like this:

	public List<T> ToObjectList3<T>(DataTable dataTable) where T : new()
    {
        List<T> returnList = new List<T>(dataTable.Rows.Count);
        List<ExtendedPropertyInfo> accessibleProperties = new List<ExtendedPropertyInfo>();

        foreach (PropertyInfo property in new T().GetType().GetProperties())
        {
            string fieldName = ((ColumnMapping)property.GetCustomAttributes(typeof(ColumnMapping), false)[0]).FieldName;
            if (dataTable.Columns.Contains(fieldName))
            {
                accessibleProperties.Add(new ExtendedPropertyInfo{
                        fieldName = fieldName,
                        property = property
                    });
            }
        }
        for (int rowIndex = 0; rowIndex < dataTable.Rows.Count; rowIndex++)
        {
            T returnObject = new T();
            foreach (ExtendedPropertyInfo eproperty in accessibleProperties)
            {
                eproperty.property.SetValue(returnObject,
                        FieldToObject(dataTable.Rows[rowIndex][eproperty.fieldName], eproperty.property.PropertyType),
                        null);
            }
            returnList.Add(returnObject);
        }
        return returnList;
    }
This works by only checking for the existence of columns once, rather than every row and saving a few reflection calls by putting them at the start of the method.

### Conclusion
The original method is fast enough that performance benefits here aren't really worth the time taken to investigate, however it's an interesting intellectual exercise.

[link0]: http://sharpdevpt.blogspot.co.uk/2010/05/convert-datatable-into-poco-using.html "Original POCO mapper post"
[link1]: http://msdn.microsoft.com/en-us/library/system.diagnostics.stopwatch(v=vs.110).aspx "MSDN Stopwatch"
[link2]:http://msdn.microsoft.com/en-us/library/system.data.datacolumncollection.contains(v=vs.85).aspx "MSDN Contains"