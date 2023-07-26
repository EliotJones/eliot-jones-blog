#Create Table SQL Server

Most guides on the SQL 'Create Table' command seem to only include the most basic arguments necessary to create a table with a few columns.

They don't go on to detail how to name constraints, create indexes and add foreign keys or similar.

If you're one of the (seemingly very few) people not using code/model-first Entity Framework I've posted this small snippet to help you. It includes how to:

+ Create and name a primary key.
+ Create and name a foreign key.
+ Create and name a default constraint.
+ Create and name a non-clustered index.
+ Cascade delete.

It does not detail how to create a composite non-clustered index (a non-clustered index on multiple columns).

So, the whole statement is here (note that having a column with the same name as the table will cause problems in EF database-first, the Question property will be renamed Question1):

    CREATE TABLE Dog
            (
                    Id				INT IDENTITY(-2147483648, 1) CONSTRAINT PK_Dog_Id PRIMARY KEY NOT NULL,
                    Title			NVARCHAR(MAX) NULL,
                    Name			NVARCHAR(MAX) NOT NULL,
                    OwnerId			INT INDEX IX_Dog_Owner NONCLUSTERED 
                    CONSTRAINT	FK_Dog_Owner FOREIGN KEY REFERENCES dbo.Owner(Id) 
                    ON DELETE CASCADE,
                    UserId			INT NOT NULL INDEX IX_Dog_User NONCLUSTERED 
                    CONSTRAINT FK_Users_Dog FOREIGN KEY REFERENCES dbo.AspNetUsers(Id),
                    CreatedDate		DATETIME2(0) NOT NULL CONSTRAINT DF_Dog_CreatedDate DEFAULT GETDATE(),
                    ModifiedDate	DATETIME2(0) NOT NULL CONSTRAINT DF_Dog_ModifiedDate DEFAULT GETDATE(),
                    RowVersion		ROWVERSION
            )

To run through a few things to note:

```INT IDENTITY(-2147483648, 1)```   
There's an [interesting debate][link0] about whether to use a GUID or integer for a primary key, I've gone with integer since to me it's more readable, but a quick Google will provide more than enough reading to the interested coder.
I've seeded this primary key with  the minimum value for INT, this means values from -2147483648 to 1 aren't wasted.

```NVARCHAR(MAX)```   
NVARCHAR should <del>generally</del> always be used for text columns because it supports Unicode. VARCHAR supports ASCII.

```INDEX IX_Question_QuestionSet NONCLUSTERED```   
Create non-clustered index on column.

```CONSTRAINT FK_QuestionSet_Question FOREIGN KEY REFERENCES dbo.QuestionSet(Id)```   
Create foreign key on a column.

```ON DELETE CASCADE```   
Further to the foreign key constraint, when you delete a record in the parent table (QuestionSet) specifies that columns referencing this record should also be deleted. The alternative is to error if linked records are present.

```DATETIME2(0)```   
The [new DateTime data type][link1] replacing deprecated DATETIME (from SQL Server 2008 up). The number in brackets is the 'fractional seconds precision', a fancy sounding way of saying the number of decimal points after the second.

```CONSTRAINT DF_Question_ModifiedDate DEFAULT GETDATE()```   
Named Default Constraint.

```RowVersion		ROWVERSION NOT NULL```   
The row version replaces TimeStamp for SQL Server 2008 onwards,  ```NOT NULL``` is not explicitly required and the column will be Not Nullable. [Read all about it][link2].

That's all of it, there's [much more][link3] that can be done with the CREATE TABLE statement and what I have may not follow best practices but it's a helpful reminder of how everything can be combined in one statement. For instance, who knew about [sparse columns][link4]?

[link0]: http://blog.codinghorror.com/primary-keys-ids-versus-guids/ "Jef Atwood on int vs guid"
[link1]: http://msdn.microsoft.com/en-gb/library/bb677335.aspx "MSDN on DateTime2 Data Type"
[link2]: http://geekswithblogs.net/TimothyK/archive/2014/01/14/introduction-to-rowversion.aspx "RowVersion guide on a blog"
[link3]: http://msdn.microsoft.com/en-gb/library/ms174979.aspx "MSDN on Create Table"
[link4]: http://msdn.microsoft.com/en-gb/library/cc280604.aspx "MSD on sparse columns"
