# IdentityServer4.Dapper.Storage
This project provides persistence for IdentityServer4 using Dapper and MS SQL Server or Azure SQL Server. 


## Main features

The library enables persistence storing for configuration and operational data the same way the EntitiyFramework library does (IdentityServer4.EntityFramework).

See this link for more documentation:
http://docs.identityserver.io/en/latest/quickstarts/7_entity_framework.html

If you are just beginning out with IdentityServer4, it is important to note that this library will help you with persistence of the IdentityServer4 settings (such as which Clients to authorize, which ApiResources and such) but anything related to **Users** is not handled in this library. 

Both libraries are made to let the developer customize ConnectionString and DatabaseSchema.
It is built for Dotnet Core (latest version as of august 2019: 2.2.6), with DependencyInjection, making customization easy.


## Getting Started

These instructions will help you get started.

There is two packages currently.

**IdentityServer4.Dapper.Storage**: This package is the library itself that IdentityServer4 uses.

**IdentityServer4.Storage.DatabaseScripts.DbUp**: This is a small project which contains SQL scripts to create databases as well as seed with some basic settings, taken from one of the IdentityServer4 quickstarts. It uses DbUp, but you can just take the SQL scripts and use them with any database migrations you might use. 
It does not depend on IdentityServer4 but in order to seed the database, it depends on IdentityServer4.Dapper.Storage.


### Prerequisites

In order to use this package you will need the standard IdentityServer4 NuGet packages as well as this specific package.

```
dotnet add package VeryGood.IdentityServer4.Dapper.Storage
dotnet add package VeryGood.IdentityServer4.Storage.DatabaseScripts.DbUp
```

Once installed, the library is in the following namespace

```
using IdentityServer4.Dapper.Storage;
```

### Get IdentityServer4.Dapper.Storage working

In order to enable the IdentityServer4.Dapper.Storage, just invoke the middleware following the standard .AddIdentityServer call.
Make sure when specifying schema, to keep it in [brackets].

```
        public void ConfigureServices(IServiceCollection services)
        {
            /*
            Configuration omitted
            */
            var connectionString = "insert your own connection string";
            var builder = services.AddIdentityServer(options =>
                {
                    options.Events.RaiseErrorEvents = true;
                    options.Events.RaiseInformationEvents = true;
                    options.Events.RaiseFailureEvents = true;
                    options.Events.RaiseSuccessEvents = true;
                    options.IssuerUri = authority;
                })
                // Add the SQL Connection for IdentityServer4.Dapper.Storage
                .AddSQLConnection(option => { option.ConnectionString = connectionString; option.DbSchema = $"[identity]"; })
                // Add the Dapper configuration stores
                .AddDapperConfigurationStore()
                // Add the Dapper operational stores
                .AddDapperOperationalStore(option =>
                {
                    option.EnableTokenCleanup = true;
                    option.TokenCleanupInterval = 3600;
                })      

                /* Configuration omitted */

        }
```

Alternative if you are running a project which don't need the entire IdentityServer4 middleware running, but you still want to be able to access the IdentityServer4 persistence stores (e.g. if you want to add new Clients from code in another project), you can add only this middleware using:

```
        public void ConfigureServices(IServiceCollection services)
        {
            /*
            Configuration omitted
            */
            services.AddIdentityServerDapperDbProviders(options =>
                {
                    options.ConnectionString = "your connection string";
                    options.DbSchema = "[your db schema]";
                });

            /* 
            Configuration omitted 
            */

        }
```

### Get IdentityServer4.Storage.DatabaseScripts.DbUp working

This library is quite simple. It has in the IIdentityServerMigrations interface one function, UpgradeDatabase

```
bool UpgradeDatabase(bool withSeed = false);
```

If UpgradeDatabase is called with withSeed = true, then it will seed the demo configuration data to the database after creating it.

The library itself does two things;
1. Ensures the Schema exists, and if not creates it.
2. Creates the tables needed by IdentityServer4.

In order to enable the IdentityServer4.Dapper.Storage, just invoke the middleware following the standard .AddIdentityServer call.
This time the schema should be specified **without* brackets. I am aware it's confusing.

```
    public void ConfigureServices(IServiceCollection services)
        {
            /*
            Configuration omitted
            */
            var connectionString = "insert your own connection string";
            var builder = services.AddIdentityServer(options =>
                {
                    options.Events.RaiseErrorEvents = true;
                    options.Events.RaiseInformationEvents = true;
                    options.Events.RaiseFailureEvents = true;
                    options.Events.RaiseSuccessEvents = true;
                    options.IssuerUri = authority;
                })
                //Add the DbUp Database Script middleware. Set ConnectionString and Schema
                .AddDbUpDatabaseScripts(options => {
                        options.ConnectionString = connectionString;
                        options.DbSchema = "my schema";
                        });
       
            /* 
            Configuration omitted 
            */

        }
```

Once this step has been done, it will use the Schema and ConnectionString provided once you call the UpgradeDatabase() function on the IIdentityServerMigrations interface.

