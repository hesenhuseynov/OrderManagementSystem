using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagementSystem.IntegrationTests
{
    public sealed  class CustomWebApplicationFactory:WebApplicationFactory<Program>
    {

        private readonly string _sqlConnectionString;
        private readonly string _redisConnectionString;

        public CustomWebApplicationFactory(string sqlConnectionString, string redisConnectionString)
        {
            _sqlConnectionString = sqlConnectionString;
            _redisConnectionString = redisConnectionString;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.UseContentRoot(GetApiProjectPath());

            builder.ConfigureAppConfiguration((_, config) =>
            {
                //config.AddInMemoryCollection(new Dictionary<string, string?>
                //{

                //    ["ConnectionStrings:DefaultConnection"] =
                //    "Server=ACER\\SQLEXPRESS01;Database=OrderManagementTestDb;Trusted_Connection=True;TrustServerCertificate=True;",

                //    ["ConnectionStrings:Redis"] =
                //    "127.0.0.1:6379,defaultDatabase=1,abortConnect=false,allowAdmin=true",

                //    ["CacheSettings:OrderByIdTtlMinutes"] = "5"
                //});

                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = _sqlConnectionString,
                    ["ConnectionStrings:Redis"] = _redisConnectionString,
                    ["CacheSettings:OrderByIdTtlMinutes"] = "5"
                });
                
            });

            base.ConfigureWebHost(builder);
        }

        private static string GetApiProjectPath()
        {
            var testProjectOutputDirectory = AppContext.BaseDirectory;

            var testProjectDirectory = Directory.GetParent(testProjectOutputDirectory)!
                .Parent!
                .Parent!
                .Parent!
                .FullName;

            var solutionDirectory = Directory.GetParent(testProjectDirectory)!.FullName;
            return Path.Combine(solutionDirectory, "OrderManagementSystem");
        }
    }
}
