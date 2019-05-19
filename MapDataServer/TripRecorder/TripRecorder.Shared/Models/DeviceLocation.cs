using System;
using System.Collections.Generic;
using System.Text;

namespace TripRecorder.Models
{
    public struct DeviceLocation
    {
        public DeviceLocation(double longitude, double latitude, double accuracy)
        {
            Longitude = longitude;
            Latitude = latitude;
            Accuracy = accuracy;
        }

        double Longitude { get; }

        double Latitude { get; }

        double Accuracy { get; }
    }
}
