Yarn
====
*yet another repository for .net*

Yarn is a generic repository pattern implementation for .Net.
Its goal is to enforce consistent approach to data persistence regardless of the underlying technology.

Here is what it currently supports:
- EF
- NHibernate
  - SQL Server
  - MySql
  - SQLite
  - Oracle
  - Postgres
- RavenDB
- MongoDB
- In-memory object storage

###Quick example of the pattern usage###

```c#
// Bind IRepository to specific implementation (this should happen during application startup)
ObjectContainer.Current.Register<IRepository, Yarn.Data.MongoDbProvider.Repository>();

// Resolve IRepository (this may happen anywhere within application)
// Let's assume we have defined entity Category
var repo = ObjectContainer.Current.Resolve<IRepository>();
var category = repo.GetById<Category, int>(1000);
var categories = repo.GetByIdList<Category, int>(new[] { 1000, 1100 });
```

###Slightly more sophisticated example utilizing multiple implementations of IRepository###

```c#
// Bind IRepository to specific implementation (this should happen during application startup)
// For this example one must provide "EF.Default.Model" application setting and "EF.Default.Connection" connection string setting
// ("EF.Default.Model" points to an assembly which contains model class definition)
ObjectContainer.Current.Register<IRepository, Yarn.Data.MongoDbProvider.Repository>("mongo");
ObjectContainer.Current.Register<IRepository, Yarn.Data.EntityFrameworkProvider.Repository>("ef");

// Resolve IRepository (this may happen anywhere within application)
// "mongo" will resolve to MongoDB implementation, while "ef" will resolve to EF implementation
var repo = ObjectContainer.Current.Resolve<IRepository>("ef");
//var repo = ObjectContainer.Current.Resolve<IRepository>("mongo");
var category = repo.GetById<Category, int>(1000);
```

###Example of NHibernate implementation of IRepository###

```c#
// With NHibernate one must specify implementation of the data context to be used with repository
// For this example one must provide "NHibernate.MySqlClient.Model" application setting and "NHibernate.MySqlClient.Connection" connection string setting
ObjectContainer.Current.Register<IRepository, Yarn.Data.NHibernateProvider.Repository>(
                                          new Yarn.Data.NHibernateProvider.Repository("nh_uow"), "nh");
ObjectContainer.Current.Register<IDataContext, Yarn.Data.NHibernateProvider.MySqlClient.MySqlDataContext>("nh_uow");

// In order to use NHibernate with SQL Server one has to bind IDataContext to the SQL Server implementation
// Similarly to the MySQL example "NHibernate.SqlClient.Model" application setting and "NHibernate.SqlClient.Connection" connection string setting should be defined
// ObjectContainer.Current.Register<IDataContext, Yarn.Data.NHibernateProvider.SqlClient.SqlDataContext>("nh_uow");

var repo = ObjectContainer.Current.Resolve<IRepository>("nh");
var categories = repo.FindAll<Category>(c => c.Name.Contains("cat"), offset: 50, limit: 10);
```

###Example of the full text search implementation with IRepository###

- EF

  ```c#
  // Currently works only with SQL Server provider for EF
  ObjectContainer.Current.Register<IRepository, Yarn.Data.EntityFrameworkProvider.FullTextRepository>();
  ObjectContainer.Current.Register<IFullTextProvider, Yarn.Data.EntityFrameworkProvider.SqlClient.SqlFullTextProvider>();
  
  var repo = ObjectContainer.Current.Resolve<IRepository>();
  var categories = ((IFullTextRepository)repo).FullText.Seach<Category>("hello world");
  ```

- NHibernate

  ```c#
  ObjectContainer.Current.Register<IRepository, Yarn.Data.NHibernateProvider.FullTextRepository>();
  ObjectContainer.Current.Register<IFullTextProvider, Yarn.Data.NHibernateProvider.LuceneClient.LuceneFullTextProvider>();
  // One can resort to use of SQL Server full text as well
  // ObjectContainer.Current.Register<IFullTextProvider, Yarn.Data.NHibernateProvider.SqlClient.SqlFullTextProvider>();
  
  var repo = ObjectContainer.Current.Resolve<IRepository>();
  var categories = ((IFullTextRepository)repo).FullText.Seach<Category>("hello world");
  ```
  
###Example of the specification pattern implementation with IRepository

```c#
// Bind IRepository to specific implementation (this should happen during application startup)
ObjectContainer.Current.Register<IRepository, Yarn.Data.EntityFrameworkProvider.Repository>();

// Resolve IRepository (this may happen anywhere within application)
var repo = ObjectContainer.Current.Resolve<IRepository>();

// Create a specification to abstract search criteria
var spec = new Specification<Category>(c => c.Name.Contains("hello")).Or(c => c.Name.Contains("world"));
var categories = repo.FindAll<Category>(spec);
```

###Example of utilizing caching with IRepository

```c#
// Implement a cache provider to support caching of your choice
public class SimpleCache : ICachedResultProvider
{
  // Implementation goes here
}

// Bind IRepository to specific implementation (this should happen during application startup)
ObjectContainer.Current.Register<IRepository, Yarn.Data.EntityFrameworkProvider.Repository>();

// Resolve IRepository (this may happen anywhere within application)
var repo = ObjectContainer.Current.Resolve<IRepository>();

// Create a repository decorator to support caching
var cachedRepo = repo.WithCache<SimpleCache>();

// Create a specification to abstract search criteria
var spec = new Specification<Category>(c => c.Name.Contains("hello")).Or(c => c.Name.Contains("world"));

// This call produces a cache miss, hence the database is hit
var categories1 = cachedRepo.FindAll<Category>(spec);

// This call produces a cache hit, hence there will be no trip to the database
var categories2 = cachedRepo.FindAll<Category>(spec);
```

###Example of passing parameters to repository/data context constructor during initialization

```c#
// Bind IRepository to specific implementation
// Constructor arguments may differ depending on a concrete implemnetations of IRepository

// Here is the example based on Entity Framework repository implementation
ObjectContainer.Current.Register<IRepository, Yarn.Data.EntityFrameworkProvider.Repository>(
  () => new Repository(lazyLoadingEnabled: false, 
                      nameOrConnectionString: "NorthwindConnection", 
                      configurationAssembly: typeof(Customer).Assembly));
                      
// Here is the example based on NHiberante repository implementation using SQL Server database backend
ObjectContainer.Current.Register<IRepository, Yarn.Data.NHibernate.Repository>();
ObjectContainer.Current.Register<IDataContext, Yarn.Data.NHibernate.SqlClient.Sql2012DataContext>(
  new Sql2012DataContext(nameOrConnectionString: "NorthwindConnection", 
                  configurationAssembly: typeof(Customer).Assembly);
```

###Example of using eager loading

```c#
// Bind IRepository to specific implementation (this should happen during application startup)
ObjectContainer.Current.Register<IRepository, Yarn.Data.EntityFrameworkProvider.Repository>();

// Resolve IRepository (this may happen anywhere within application)
var repo = ObjectContainer.Current.Resolve<IRepository>();

// Load customer with orders and order details
var customer = repo.As<ILoadServiceProvider>()
                  .Load<Customer>()
                  .Include(c => c.Orders)
                  .Include(c => c.Orders.Select(o => o.Order_Details))
                  .Find(c => c.CustomerID == "ALFKI");
```
