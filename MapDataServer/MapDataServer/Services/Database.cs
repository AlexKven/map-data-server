﻿using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using MapDataServer.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MapDataServer.Services
{
    public class Database : LinqToDB.Data.DataConnection, IDatabase
    {
        public Database(IConfiguration config, IDataProvider dataProvider)
            : base(dataProvider, config.GetConnectionString("MySQL"))
        {
            Initializer = InitializeAsync();
        }

        public Task Initializer { get; }

        public ITable<MapRegion> MapRegions => GetTable<MapRegion>();

        //public Task BeginTransactionAsync()
        //{
        //    throw new NotImplementedException();
        //}

        //public Task EndTransactionAsync()
        //{
        //    throw new NotImplementedException();
        //}

        private async Task InitializeAsync()
        {
            var sp = DataProvider.GetSchemaProvider();
            var tableTypes = sp.GetSchema(this).Tables.Select(table => table.TableName);
            if (!tableTypes.Contains("MapRegions"))
            {
                await this.CreateTableAsync<MapRegion>();
            }
        }
    }
}
