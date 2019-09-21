using LinqToDB;
using MapDataServer.Models;
using MapDataServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace MapDataServer.Controllers
{
    [Route("trip")]
    [ApiController]
    public class TripDataController : BaseController
    {
        private IDatabase Database { get; }

        public TripDataController(IDatabase database, IConfiguration configuration)
            :base(configuration)
        {
            Database = database;
        }

        [HttpPost("start")]
        public async Task<ActionResult> StartTrip([FromBody] Trip trip)
        {
            await Database.Initializer;
            if (!IsAuthorized())
                return Unauthorized();

            trip.InProgress = true;
            var idBytes = new byte[8];
            new Random().NextBytes(idBytes);
            trip.Id = BitConverter.ToInt64(idBytes, 0);
            await Database.InsertAsync(trip);
            return new JsonResult(trip);
        }

        [HttpPost("end")]
        public async Task<ActionResult> EndTrip([FromQuery] long tripId, [FromQuery] DateTime endTime)
        {
            await Database.Initializer;
            if (!IsAuthorized())
                return Unauthorized();

            var trip = await Database.Trips.Where(t => t.Id == tripId).FirstOrDefaultAsync();
            if (trip == null)
                return NotFound();

            trip.EndTime = endTime;
            await Database.InsertOrReplaceAsync(trip);
            return Ok();
        }

        [HttpPost("setObaTrip")]
        public async Task<ActionResult> SetObaTrip([FromQuery] long tripId, [FromQuery] string obaTripId, [FromQuery] string obaVehicleId = null)
        {
            await Database.Initializer;
            var link = new ObaTripLink() { MapTripId = tripId, ObaTripId = obaTripId, ObaVehicleId = obaVehicleId };
            await Database.InsertOrReplaceAsync(link);
            return Ok();
        }

        [HttpPost("point")]
        public async Task<ActionResult> PostPoint([FromBody] TripPoint point)
        {
            await Database.Initializer;
            if (!IsAuthorized())
                return Unauthorized();

            return new JsonResult(InsertPointAndGenerateId(point));
        }

        [HttpPost("points")]
        public async Task<ActionResult> PostPoints([FromBody] TripPoint[] points)
        {
            await Database.Initializer;
            if (!IsAuthorized())
                return Unauthorized();

            if (points.Length == 0)
                return NoContent();

            var results = new TripPoint[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                results[i] = await InsertPointAndGenerateId(points[i]);
            }

            return new JsonResult(results);
        }

        private async Task<TripPoint> InsertPointAndGenerateId(TripPoint point)
        {
            var idBytes = new byte[8];
            new Random().NextBytes(idBytes);
            point.Id = BitConverter.ToInt64(idBytes, 0);
            await Database.InsertAsync(point);
            return point;
        }
    }
}
