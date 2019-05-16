using LinqToDB;
using MapDataServer.Models;
using MapDataServer.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace MapDataServer.Controllers
{
    [Route("trip")]
    [ApiController]
    public class TripDataController : ControllerBase
    {
        private IDatabase Database { get; }

        public TripDataController(IDatabase database)
        {
            Database = database;
        }

        [HttpPost("start")]
        public async Task<ActionResult<string>> StartTrip([FromBody] Trip trip)
        {
            await Database.Initializer;
            trip.InProgress = true;
            var idBytes = new byte[8];
            new Random().NextBytes(idBytes);
            trip.Id = BitConverter.ToInt64(idBytes, 0);
            await Database.InsertAsync(trip);
            return new JsonResult(trip);
        }

        [HttpPost("point")]
        public async Task<ActionResult<string>> PostPoint([FromBody] TripPoint point)
        {
            await Database.Initializer;
            var idBytes = new byte[8];
            new Random().NextBytes(idBytes);
            point.Id = BitConverter.ToInt64(idBytes, 0);
            await Database.InsertAsync(point);
            return new JsonResult(point);
        }
    }
}
