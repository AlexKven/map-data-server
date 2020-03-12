using LinqToDB;
using MapDataServer.Helpers;
using MapDataServer.Models;
using MapDataServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MapDataServer.Controllers
{
    [Route("trip")]
    [ApiController]
    public class TripDataController : BaseController
    {
        private IDatabase Database { get; }
        private ITripPreprocessor TripPreprocessor { get; }

        public TripDataController(IDatabase database, IConfiguration configuration, ITripPreprocessor tripPreprocessor)
            :base(configuration)
        {
            Database = database;
            TripPreprocessor = tripPreprocessor;
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
            point.Id = new Random().RandomLong();
            await Database.InsertAsync(point);
            return point;
        }

        [HttpGet("tripSummary")]
        public async Task<ActionResult> TripSummary([FromQuery] long tripId)
        {
            var preprocessed = await Database.PreprocessedTrips.Where(trip => trip.Id == tripId).ToArrayAsync();
            if (preprocessed.Any())
                return new JsonResult(preprocessed[0]);
            var result = await TripPreprocessor.PreprocessTrip(tripId, CancellationToken.None);
            return new JsonResult(result);
        }

        [HttpGet("activitySummary")]
        public async Task<ActionResult> ActivitySummary([FromQuery] DateTime startTime, [FromQuery] DateTime endTime)
        {
            if (startTime > endTime)
                return BadRequest();
            if (endTime - startTime > TimeSpan.FromDays(366))
                return BadRequest();

            var hovTypesCount = Enum.GetValues(typeof(HovStatus)).Length;
            uint[] distances = new uint[hovTypesCount];
            TimeSpan[] times = new TimeSpan[hovTypesCount];

            DateTime beginTime = DateTime.UtcNow;
            var trips = await (from trip in Database.Trips
                         where trip.StartTime >= startTime &&
                                trip.EndTime < endTime
                         select trip).ToArrayAsync();
            var preprocessedTripCount = 0;

            foreach (var trip in trips)
            {
                var preprocessed = await Database.PreprocessedTrips.Where(processed => processed.Id == trip.Id).FirstOrDefaultAsync();
                if (preprocessed == null)
                    continue;
                preprocessedTripCount++;
                var hovStatus = trip.HovStatus;
                if (hovStatus <= HovStatus.Motorcycle && await Database.ObaTripLinks.AnyAsync(oba => oba.MapTripId == trip.Id))
                    hovStatus = HovStatus.Transit;
                distances[(int)hovStatus] += preprocessed.DistanceMeters;
                times[(int)hovStatus] += (preprocessed.ActualEndTime - preprocessed.ActualStartTime);
            }

            var totalDistance = distances.Sum(val => val);
            var totalTime = TimeSpan.FromMilliseconds(times.Sum(val => val.TotalMilliseconds));

            var resultBuilder = new StringBuilder();

            resultBuilder.AppendLine($"Results from {preprocessedTripCount} out of {trips.Length} trips. Others pending preprocessing.");
            resultBuilder.AppendLine();
            resultBuilder.AppendLine($"Total distance: {totalDistance} meters");
            resultBuilder.AppendLine($"Total time: {totalTime}");
            resultBuilder.AppendLine();

            for (int i = 0; i < hovTypesCount; i++)
            {
                var distance = distances[i];
                var distFraction = (double)distance / (double)totalDistance;
                var time = times[i];
                var timeFraction = time / totalTime;
                resultBuilder.AppendLine($"For {(HovStatus)i}:");
                resultBuilder.AppendLine($"Distance: {distance} meters ({distFraction.ToString("P")})");
                resultBuilder.AppendLine($"Time: {time} ({timeFraction.ToString("P")})");
                resultBuilder.AppendLine();
            }

            resultBuilder.AppendLine($"Server time: {DateTime.UtcNow - beginTime}");
            return new OkObjectResult(resultBuilder.ToString());
        }
    }
}