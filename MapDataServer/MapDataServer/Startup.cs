﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using LinqToDB.Configuration;
using LinqToDB.DataProvider;
using MapDataServer.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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

            services.AddSingleton<LinqToDB.DataProvider.MySql.MySqlDataProvider, LinqToDB.DataProvider.MySql.MySqlDataProvider>()
            .AddSingleton<IDataProvider>(svc => svc.GetService<LinqToDB.DataProvider.MySql.MySqlDataProvider>())
            .AddSingleton<IDatabase, Database>()
            .AddSingleton<IMapDownloader, MapDownloader>()
            .AddSingleton<ITripProcessorStatus, TripProcessorStatus>()
            .AddSingleton<ITripPreprocessor, TripPreprocessor>()
            .AddHostedService<TripProcessorService>()
            .AddHttpClient();
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
            
            //app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
