using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TripRecorder.Models;

namespace TripRecorder.Droid.Services
{
    public interface ILocationProvider
    {
        Task Initialize();

        Task<DeviceLocation> GetLocation();
    }
}