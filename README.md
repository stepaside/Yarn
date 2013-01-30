Yarn
====
*yet another repository for .net*

Yarn is a generic repository pattern implementation for .Net.
It tries to "enforce" the same approach to data persistence regardless of the underlying technology.

Here is what it currently supports:
- EF
- NHibernate
  - SQL Server
  - MySql
  - SQLite
  - Oracle
  - Postgres
- RavendDB
- MongoDB

###Quick example of the pattern usage###

```c#
// Bind IRepository to specific implementation (this should happen during application startup)
ObjectFactory.Bind<IRepository, Yarn.Data.MongoDbProvider.Repository>();

// Resolve IRepository (this may happen anywhere within application)
// Let's assume we have defined entity Category
var repo = ObjectFactory.Resolve<IRepository>();
var category = repo.GetById<Category, int>(1000);
```

###Slightly more sophisticated example utilizing multiple implementations of IRepository###

```c#
// Bind IRepository to specific implementation (this should happen during application startup)
ObjectFactory.Bind<IRepository, Yarn.Data.MongoDbProvider.Repository>("mongo");
ObjectFactory.Bind<IRepository, Yarn.Data.EntityFrameworkProvider.Repository>("ef");

// Resolve IRepository (this may happen anywhere within application)
// "mongo" will resolve to MongoDB implementation, while "ef" will resolve to EF implementation
var repo = ObjectFactory.Resolve<IRepository>("ef");
//var repo = ObjectFactory.Resolve<IRepository>("mongo");
var category = repo.GetById<Category, int>(1000);
```

###Example of NHibernate implementation of IRepository###

```c#
// With NHibernate one must specify implementation of the data context to be used with repository
ObjectFactory.Bind<IRepository, Yarn.Data.NHibernateProvider.Repository>();
ObjectFactory.Bind<IDataContext, Yarn.Data.NHibernateProvider.MySqlClient.MySqlDataContext>();

var repo = ObjectFactory.Resolve<IRepository>();
var categories = repo.FindAll<Category>(c => c.Name.Contains("cat"));
```

###Example of the full text search implementation with IRepository###

- EF

  ```c#
  // Currently works only with SQL Server provider for EF
  ObjectFactory.Bind<IRepository, Yarn.Data.EntityFrameworkProvider.FullTextRepository>();
  ObjectFactory.Bind<IFullTextProvider, Yarn.Data.EntityFrameworkProvider.SqlClient.SqlFullTextProvider>();
  
  var repo = ObjectFactory.Resolve<IRepository>();
  var categories = ((IFullTextRepository)repo).FullText.Seach<Category>("hello world");
  ```

- NHibernate

  ```c#
  ObjectFactory.Bind<IRepository, Yarn.Data.NHibernateProvider.FullTextRepository>();
  ObjectFactory.Bind<IFullTextProvider, Yarn.Data.NHibernateProvider.LuceneClient.LuceneFullTextProvider>();
  // One can resort to use of SQL Server full text as well
  // ObjectFactory.Bind<IFullTextProvider, Yarn.Data.NHibernateProvider.SqlClient.SqlFullTextProvider>();
  
  var repo = ObjectFactory.Resolve<IRepository>();
  var categories = ((IFullTextRepository)repo).FullText.Seach<Category>("hello world");
  ```
  
###Example of the specification pattern implementation with IRepository

```c#
// Bind IRepository to specific implementation (this should happen during application startup)
ObjectFactory.Bind<IRepository, Yarn.Data.EntityFrameworkProvider.Repository>();

// Resolve IRepository (this may happen anywhere within application)
var repo = ObjectFactory.Resolve<IRepository>();

// Create a specification to abstract search criteria
var spec = new Specification<Category>(c => c.Name.Contains("hello")).Or(c => c.Name.Contains("world"));
var categories = repo.FindAll<Category>(spec);
```

###Example of utilizing caching with IRepository

```c#
// Implement a cache provider to support caching of your choice
public class SimpleCache : ICacheProvider
{
  // Implementation goes here
}

// Bind IRepository to specific implementation (this should happen during application startup)
ObjectFactory.Bind<IRepository, Yarn.Data.EntityFrameworkProvider.Repository>();

// Resolve IRepository (this may happen anywhere within application)
var repo = ObjectFactory.Resolve<IRepository>();

// Create a repository decorator to support caching
var cachedRepo = repo.UseCache<SimpleCache>();

// Create a specification to abstract search criteria
var spec = new Specification<Category>(c => c.Name.Contains("hello")).Or(c => c.Name.Contains("world"));

// This call produces a cache miss, hence the database is hit
var categories1 = cachedRepo.FindAll<Category>(spec);

// This call produces a cache hit, hence there will be not trip to the database
var categories2 = cachedRepo.FindAll<Category>(spec);
```
