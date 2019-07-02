using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MapDataServer.Services
{
    public interface IMapDownloader
    {
        Task DownloadMapRegions(int minLon, int minLat, int lengthLon, int lengthLat);
    }
}
