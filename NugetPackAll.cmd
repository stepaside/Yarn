rmdir NugetPackages
mkdir NugetPackages
.nuget\nuget.exe pack Yarn\Yarn.nuspec -BasePath Yarn\bin\Release -OutputDirectory NugetPackages
.nuget\nuget.exe pack Yarn.EF\Yarn.EF.nuspec -BasePath Yarn.EF\bin\Release -OutputDirectory NugetPackages
.nuget\nuget.exe pack Yarn.InMemory\Yarn.InMemory.nuspec -BasePath Yarn.InMemory\bin\Release -OutputDirectory NugetPackages
.nuget\nuget.exe pack Yarn.MongoDb\Yarn.MongoDb.nuspec -BasePath Yarn.MongoDb\bin\Release -OutputDirectory NugetPackages
.nuget\nuget.exe pack Yarn.NHibernate\Yarn.NHibernate.nuspec -BasePath Yarn.NHibernate\bin\Release -OutputDirectory NugetPackages
.nuget\nuget.exe pack Yarn.RavenDb\Yarn.RavenDb.nuspec -BasePath Yarn.RavenDb\bin\Release -OutputDirectory NugetPackages
.nuget\nuget.exe pack Yarn.Cache\Yarn.Cache.nuspec -BasePath Yarn.Cache\bin\Release -OutputDirectory NugetPackages
.nuget\nuget.exe pack Yarn.StructureMap\Yarn.StructureMap.nuspec -BasePath Yarn.StructureMap\bin\Release -OutputDirectory NugetPackages
.nuget\nuget.exe pack Yarn.Nemo\Yarn.Nemo.nuspec -BasePath Yarn.Nemo\bin\Release -OutputDirectory NugetPackages
.nuget\nuget.exe pack Yarn.Elasticsearch\Yarn.Elasticsearch.nuspec -BasePath Yarn.Elasticsearch\bin\Release -OutputDirectory NugetPackages