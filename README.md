Yarn
====

yet another repository for .net

Generic repository pattern implementation. 

Currently supports
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
// Bind IRepository to to specific implementation (this should happen during application startup)
ObjectFactory.Bind<IRepository, Yarn.Data.MongoDbProvider.Repository>();

// Resolve IRepository (this may happen anywhere within application)
// Let's assume we have defined entity Category
var repo = ObjectFactory.Resolve<IRepository>();
var category = repo.GetById<Category, int>(1000);
```

###Slightly more sophisticated example utilizing multiple implementations of IRepository###

```c#
// Bind IRepository to to specific implementation (this should happen during application startup)
ObjectFactory.Bind<IRepository, Yarn.Data.MongoDbProvider.Repository>("mongo");
ObjectFactory.Bind<IRepository, Yarn.Data.EntityFrameworkProvider.Repository>("ef");

// Resolve IRepository (this may happen anywhere within application)
// "mongo" will resolve to MongoDB implementation, while "ef" will resolve to EF implementation
var repo = ObjectFactory.Resolve<IRepository>("ef");
//var repo = ObjectFactory.Resolve<IRepository>("mongo");
var category = repo.GetById<Category, int>(1000);
```

###Example of the full text search implementation with IRepository###

```c#
// Currently works only with SQL Server provider for EF
ObjectFactory.Bind<IRepository, Yarn.Data.EntityFrameworkProvider.FullTextRepository>();
ObjectFactory.Bind<IFullTextProvider, Yarn.Data.EntityFrameworkProvider.SqlClient.SqlFullTextProvider>();

var repo = ObjectFactory.Resolve<IRepository>();
var categories = repo.FullText.Seach<Category>("hello world");
```

###Example of NHibernate implementation of IRepository###

```c#
// With NHibernate one must specify implementation of the data context to be used with repository
ObjectFactory.Bind<IRepository, Yarn.Data.NHibernateProvider.Repository>();
ObjectFactory.Bind<IDataContext, Yarn.Data.NHibernateProvider.MySqlClient.MySqlDataContext>();

var repo = ObjectFactory.Resolve<IRepository>();
var categories = repo.FindAll<Category>(c => c.Name.Contains("cat"));
```
