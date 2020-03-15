using System;
using System.Collections.Generic;
using System.Text;

namespace MapDataServer.Models
{
    public class PaginatedResponse<T>
    {
        public int Start { get; set; }
        public int Count { get; set; }
        public int Total { get; set; }
        public T[] Items { get; set; }
    }
}
