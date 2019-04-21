using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace MapDataServer.Utilities
{
    public class DbRowParameter : IDbRowParameter
    {
        public string Name { get; set; }
        public bool? NotNull { get; set; }
        public object Default { get; set; }
        public bool AutoIncrement { get; set; }
        public bool PrimaryKey { get; set; }
        public DbRowParameter() { }
        public DbRowParameter(string name)
        {
            Name = name;
        }

        public IDbRowParameter MakeNotNull()
        {
            NotNull = true;
            return this;
        }

        public IDbRowParameter BlockNotNull()
        {
            NotNull = false;
            return this;
        }

        public IDbRowParameter MakeAutoIncrement()
        {
            AutoIncrement = true;
            return this;
        }

        public IDbRowParameter MakePrimaryKey()
        {
            PrimaryKey = true;
            return this;
        }

        public IDbRowParameter SetDefault(object _default)
        {
            Default = _default;
            return this;
        }
    }
}
