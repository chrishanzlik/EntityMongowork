# EntityMongowork

> uses MongoDB in a entity framework way.

---

**Warning:** This is just a early conceptual version. Currently there is no change tracking available. Most of the `MongoDbSet<T>` methods will replaced by LINQ / `IQueryable<T>` in future versions.

Example:

``` cshap

public class MyDbContext : MongoDbContext
{
	public virtual MongoDbSet<Person> Persons { get; set; }
}

public async Task Main()
{
	var ctx = new MyDbContext(connectionString, databaseName);

	var person = await ctx.Persons.GetByIdAsync(id);

	ctx.Persons.Insert(person1);
	ctx.Persons.Insert(person2);

	ctx.SaveChangesAsync();
}

```

