using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TripRecorder2.Services
{
    public interface ILocationProvider
    {
        Task<bool> CheckPermission();
    }
}
