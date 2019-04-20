using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MapDataServer.Services
{
    public interface IDatabaseConnection : IDisposable
    {
        Task OpenAsync();
        Task CloseAsync();
    }
}
