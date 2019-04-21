using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MapDataServer.Converters;
using MapDataServer.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MapDataServer.Helpers;

namespace MapDataServer
{
    public class DiTest
    {
        public DiTest(IServiceProvider provider)
        {
            var _int = provider.GetService<IDbType<int>>();
            var _bool = provider.GetService<IDbType<bool>>();
            var _n_int = provider.GetService<IDbType<int?>>();
            var _double = provider.GetService<IDbType<double>>();
        }
    }

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
            var genericIDbType = typeof(IDbType<>).MakeGenericType(typeof(Nullable<>));

            services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>(provider =>
                new DbConnectionFactory(provider.GetService<IConfiguration>().GetConnectionString("MySQL")))
            .AddSingleton<IDbType<int>>(new DbType<int>("INT", val => int.Parse(val), val => val.ToString(), 0, true))
            .AddSingleton<IDbType<bool>>(new DbType<bool>("BOOLEAN", val => bool.Parse(val), val => val ? "TRUE" : "FALSE", false, true))
            .AddSingleton<IDbType<double>>(new DbType<double>("DOUBLE", val => double.Parse(val), val =>
            {
                if (double.IsNaN(val))
                    return null;
                return val.ToString();
            }, double.NaN, true))
            .AddSingleton<IDbType<float>>(new DbType<float>("FLOAT", val => float.Parse(val), val => val.ToString(), float.NaN, true))
            .AddSingleton<IDbType<int?>, DependencyInjectedNullableDbType<int>>()
            .AddSingleton<DiTest>();
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
