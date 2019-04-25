﻿using LinqToDB;
using LinqToDB.Data;
using MapDataServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MapDataServer.Services
{
    public interface IDatabase : IDataContext
    {
        ITable<MapRegion> MapRegions { get; }
        Task Initializer { get; }
    }
}
