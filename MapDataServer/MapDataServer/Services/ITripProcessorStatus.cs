﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MapDataServer.Services
{
    public interface ITripProcessorStatus
    {
        int RunCount { get; set; }
    }
}
