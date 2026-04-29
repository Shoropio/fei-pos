using System;
using System.Data.Common;
using System.Linq;
using FeiPos.Domain.Entities;
using FeiPos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FeiPos.Infrastructure.Persistence
{
    public static class DbInitializer
    {
        private const string InitialCreateMigrationId = "20260429000000_InitialCreate";

        public static void Initialize(AppDbContext context)
        {
            MarkInitialMigrationAppliedForLegacyDatabase(context);
            context.Database.Migrate();

            if (context.Products.Any()) return; // DB ya tiene datos

            var products = new Product[]
            {
                new Product { Name = "Café Gourmet 250g", Sku = "COF001", Price = 4500, TaxRate = 13, Stock = 50, Barcode = "123456" },
                new Product { Name = "Pan Artesanal", Sku = "BAK001", Price = 1200, TaxRate = 1, Stock = 20, Barcode = "789012" },
                new Product { Name = "Leche Semidescremada 1L", Sku = "DAI001", Price = 950, TaxRate = 1, Stock = 100, Barcode = "000000" },
                new Product { Name = "Refresco Natural 500ml", Sku = "BEV001", Price = 1500, TaxRate = 13, Stock = 40, Barcode = "111111" }
            };

            context.Products.AddRange(products);
            context.SaveChanges();
        }

        private static void MarkInitialMigrationAppliedForLegacyDatabase(AppDbContext context)
        {
            context.Database.OpenConnection();

            try
            {
                var connection = context.Database.GetDbConnection();

                if (!TableExists(connection, "Products") || TableExists(connection, "__EFMigrationsHistory"))
                {
                    return;
                }

                context.Database.ExecuteSqlRaw("""
                    CREATE TABLE "__EFMigrationsHistory" (
                        "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                        "ProductVersion" TEXT NOT NULL
                    );
                    """);

                context.Database.ExecuteSqlRaw("""
                    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
                    VALUES ('20260429000000_InitialCreate', '8.0.4');
                    """);
            }
            finally
            {
                context.Database.CloseConnection();
            }
        }

        private static bool TableExists(DbConnection connection, string tableName)
        {
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT COUNT(*)
                FROM sqlite_master
                WHERE type = 'table' AND name = $tableName;
                """;

            var parameter = command.CreateParameter();
            parameter.ParameterName = "$tableName";
            parameter.Value = tableName;
            command.Parameters.Add(parameter);

            return Convert.ToInt32(command.ExecuteScalar()) > 0;
        }
    }
}
