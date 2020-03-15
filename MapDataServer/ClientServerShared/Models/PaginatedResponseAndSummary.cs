using System;
using System.Collections.Generic;
using System.Text;

namespace MapDataServer.Models
{
    public class PaginatedResponseAndSummary<T, S> : PaginatedResponse<T>
    {
        public S Summary { get; set; }
    }
}
