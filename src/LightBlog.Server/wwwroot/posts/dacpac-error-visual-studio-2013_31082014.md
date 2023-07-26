#Error Creating a DACPAC in Visual Studio 2013 from SQL Server 2014

Working on projects in your spare time is great because you're free to pursue every little diversion; it means you make no real progress with your actual project but you learn a lot.

When I had to add a column to one of my database tables I decided to investigate creating a [DACPAC][link0](1). A DACPAC allows you to turn your existing database into a database project in Visual Studio which means your schema can be placed under source control. Changes can then be pushed to your database in a friendly wizard format.

To create a DACPAC you right click on your existing database and choose *Extract Data Tier Application* under tasks:

<img src="/images/dacpac/ExtractDacpac.png" alt="Right click Database, choose tasks." />

The wizard then runs through the steps to create the DACPAC.

Next create a new SQL Server Database project in your Visual Studio solution:

<img src="/images/dacpac/CreateDatabaseProject.png" alt="Screenshot of creating new Database Project"/>


Then right click the project and choose *Import -> Data-Tier Application*. Choose the dacpac you extracted from SQL Server and then click *Start*.

If everything goes according to plan you should successfully import the dacpac. If this is the case, [use this post][link0] to learn what you can do with your dacpac.

However I got the following error:
    
    Internal Error. The database platform service with type Microsoft.Data.Tools.
    Schema.Sql.Sql120DatabaseSchemaProvider is not valid. You must make sure the
    service is loaded, or you must provide the full type name of a valid database
    platform service.

I'm using Visual Studio 2013 Express with SQL Server 2014 Express (I'm poor :) ), after a bit of searching I found the problem was an outdated version of the Data Tools. To fix this do the following, click *Tools -> Extensions and Updates*:

<img src="/images/dacpac/UpdateVisualStudio.png" alt="Screenshot of updating Visual Studio"/>

Then choose *Updates* from the window. You should see an update with a name similar to SQL Server Data Tools in *Product Updates* (I've already updated mine so it's not shown):

<img src="/images/dacpac/UpdateVisualStudioDataTools.png" alt="Screenshot of updating Visual Studio 2013 data tools"/>

Hopefully this fixes your issue.

1. Thanks to Steve Wade for telling me about dacpacs.

[link0]: http://sqlblog.com/blogs/jamie_thomson/archive/2014/01/18/dacpac-braindump.aspx "Everything You Need to Know about DacPacs"