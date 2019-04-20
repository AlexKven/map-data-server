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
        public bool Default { get; set; }
        public bool AutoIncrement { get; set; }
        public bool PrimaryKey { get; set; }
    }
}
