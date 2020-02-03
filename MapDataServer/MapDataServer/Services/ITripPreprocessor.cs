using MapDataServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MapDataServer.Services
{
    public interface ITripPreprocessor
    {
        Task<PreprocessedTrip> PreprocessTrip(long tripId);
    }
}
