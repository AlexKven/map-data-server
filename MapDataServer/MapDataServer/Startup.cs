using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;

namespace MapDataServer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            DbConnect();
        }

        async void DbConnect()
        {
            MySql.Data.MySqlClient.MySqlConnection connection = new MySql.Data.MySqlClient.MySqlConnection(
                "Data Source='mapdataserverdb.ckzhqptufd8c.us-east-2.rds.amazonaws.com';Port=3306;Database='map_data';UID='admin';PWD='hijklmnop';");
            await connection.OpenAsync();
            var database = connection.Database;
            //using (var command = new MySqlCommand("CREATE TABLE IF NOT EXISTS map_regions(long_region SMALLINT, lat_region SMALLINT, PRIMARY KEY (long_region, lat_region));", connection))
            //{
            //    var result = await command.ExecuteNonQueryAsync();
            //}
            //using (var command = new MySqlCommand("INSERT INTO map_regions VALUES(0, 0);", connection))
            //{
            //    var result = await command.ExecuteNonQueryAsync();
            //}
            using (var command = new MySqlCommand("SELECT COUNT(CASE WHEN long_region = 0 AND lat_region = 1 THEN 1 END) FROM map_regions;", connection))
            {
                var result = await command.ExecuteScalarAsync();
                
            }
            await connection.CloseAsync();
            connection.Dispose();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
