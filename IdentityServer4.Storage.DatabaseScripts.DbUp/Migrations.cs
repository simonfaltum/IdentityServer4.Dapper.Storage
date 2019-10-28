using DbUp;
using DbUp.Helpers;
using System;
using System.Collections.Generic;
using System.Reflection;
using IdentityServer4.Storage.DatabaseScripts.DbUp.Interfaces;
using IdentityServer4.Storage.DatabaseScripts.DbUp.Options;

namespace IdentityServer4.Storage.DatabaseScripts.DbUp
{
    public class Migrations : IIdentityServerMigrations
    {
        private readonly IIdentityServerConfigService identityServerConfigHandler;
        private readonly string _connectionString;
        private readonly string _schema;

        public Migrations(DBProviderOptions options, IIdentityServerConfigService identityServerConfigHandler)
        {

            this.identityServerConfigHandler = identityServerConfigHandler;
            _schema = options.DbSchema;
            _connectionString = options.ConnectionString;

        }


        public bool UpgradeDatabase(bool withSeed = false)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Trying to upgrade all databases");
            Console.ResetColor();
            EnsureDatabase.For.SqlDatabase(_connectionString);
            var fullSuccess = true;
            var result = UpgradeDatabase(_connectionString, _schema, "IdentityServerScripts");
            if (result == -1)
                fullSuccess = false;
            if (withSeed)
            {
                identityServerConfigHandler.SetupIdentityDefaultConfig();
            }

            return fullSuccess;
        }

        public static int EnsureSchema(string connectionString, string schema)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Preparing to upgrade {schema}");
            var variableSubstitutions = new Dictionary<string, string>();
            variableSubstitutions.Add("schemaname", $"{schema}");

            Console.ResetColor();
            var upgradeEngine = DeployChanges.To
                .SqlDatabase(connectionString)
                .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly(), (s) => s.Contains("EveryRun"))
                .WithVariable("schemaname", $"{schema}")
                .JournalTo(new NullJournal())
                .WithTransaction()
                .LogToConsole();

            var upgrader = upgradeEngine.Build();
            if (upgrader.IsUpgradeRequired())
            {
                var result = upgrader.PerformUpgrade();
                if (!result.Successful)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(result.Error);
                    Console.ResetColor();
#if DEBUG
                    Console.ReadLine();
#endif
                    return -1;
                }
            }


            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Success!");
            Console.ResetColor();
            return 0;
        }


        public static int UpgradeDatabase(string connectionString, string schema, string scriptFolder)
        {
            EnsureSchema(connectionString, schema);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Preparing to upgrade {schema}");
            var variableSubstitutions = new Dictionary<string, string>();
            variableSubstitutions.Add("schemaname", $"{schema}");

            Console.ResetColor();
            var upgradeEngine = DeployChanges.To
                .SqlDatabase(connectionString)
                .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly(), (s) => s.Contains(scriptFolder))
                .WithVariable("schemaname", $"{schema}")
                .JournalToSqlTable(schema, "SchemaVersions")
                .WithTransaction()
                .LogToConsole();

            var upgrader = upgradeEngine.Build();
            if (upgrader.IsUpgradeRequired())
            {
                var result = upgrader.PerformUpgrade();
                if (!result.Successful)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(result.Error);
                    Console.ResetColor();
#if DEBUG
                    Console.ReadLine();
#endif
                    return -1;
                }
            }


            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Success!");
            Console.ResetColor();


            return 0;


        }

    }
}