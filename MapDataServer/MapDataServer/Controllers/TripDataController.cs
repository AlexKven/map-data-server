﻿using LinqToDB;
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
        public async Task<ActionResult> ActivitySummary([FromQuery] DateTime startTime, [FromQuery] DateTime endTime, [FromQuery] bool textSummary = false)
        {
            if (startTime > endTime)
                return BadRequest();
            if (endTime - startTime > TimeSpan.FromDays(366))
                return BadRequest();

            ActivitySummary summary = new ActivitySummary();

            DateTime beginTime = DateTime.UtcNow;
            var trips = await (from trip in Database.Trips
                         where trip.StartTime >= startTime &&
                                trip.EndTime < endTime
                         select trip).ToArrayAsync();

            foreach (var trip in trips)
            {
                if (trip.HovStatus <= HovStatus.Motorcycle && await Database.ObaTripLinks.AnyAsync(oba => oba.MapTripId == trip.Id))
                    trip.HovStatus = HovStatus.Transit;
                var preprocessed = await Database.PreprocessedTrips.Where(processed => processed.Id == trip.Id).FirstOrDefaultAsync();

                summary.AddTrip(trip, preprocessed);
            }

            if (textSummary)
            {
                var resultBuilder = new StringBuilder();
                resultBuilder.Append(summary.ToString());
                resultBuilder.AppendLine($"Server time: {DateTime.UtcNow - beginTime}");
                return new OkObjectResult(resultBuilder.ToString());
            }
            return new OkObjectResult(summary);
        }

        [HttpGet("tripsForTimeRange")]
        public async Task<ActionResult> TripsForTimeRange(
            [FromQuery] DateTime startTime, [FromQuery] DateTime endTime,
            [FromQuery] int start = 0)
        {
            if (startTime > endTime)
                return BadRequest();
            if (endTime - startTime > TimeSpan.FromDays(2000))
                return BadRequest();

            var hovTypesCount = Enum.GetValues(typeof(HovStatus)).Length;
            uint[] distances = new uint[hovTypesCount];
            TimeSpan[] times = new TimeSpan[hovTypesCount];

            var total = await (from trip in Database.Trips
                              where trip.StartTime >= startTime &&
                                     trip.EndTime < endTime
                              select trip).CountAsync();
            var fullQuery = from trip in Database.Trips
                        join processed in Database.PreprocessedTrips
                        on trip.Id equals processed.Id into p
                        from processed in p.DefaultIfEmpty()
                        where trip.StartTime >= startTime &&
                               trip.EndTime < endTime
                        orderby trip.StartTime ascending
                        select new TripSummary(trip, processed);

            var query = fullQuery.Skip(start).Take(100);

            DateTime beginTime = DateTime.UtcNow;
            var trips = await query.ToArrayAsync();

            return new OkObjectResult(new PaginatedResponse<TripSummary>()
            {
                Total = total,
                Count = trips.Length,
                Start = start,
                Items = trips
            });
        }

        private static (T1, T2) MakeTuple<T1, T2>(T1 item1, T2 item2) => (item1, item2);

        [HttpGet("pointsForTrip")]
        public async Task<ActionResult> PointsForTrip([FromQuery] long tripId, [FromQuery] int start = 0,
            [FromQuery] bool includeObaPoints = false)
        {
            var total = await (from point in Database.TripPoints
                               where point.TripId == tripId
                               select point).CountAsync();
            if (includeObaPoints)
            {
                var fullQuery = from point in Database.TripPoints
                                where point.TripId == tripId
                                join obaPoint in Database.ObaTripPointLinks
                                on point.Id equals obaPoint.Id into p
                                from obaPoint in p.DefaultIfEmpty()
                                orderby point.Time ascending
                                select MakeTuple(point, obaPoint);
                var query = fullQuery.Skip(start).Take(100);
                var points = await query.ToArrayAsync();

                return new OkObjectResult(new PaginatedResponse<(TripPoint, ObaTripPointLink)>()
                {
                    Total = total,
                    Count = points.Length,
                    Start = start,
                    Items = points
                });
            }
            else
            {
                var fullQuery = from point in Database.TripPoints
                                where point.TripId == tripId
                                orderby point.Time ascending
                                select point;
                var query = fullQuery.Skip(start).Take(100);
                var points = await query.ToArrayAsync();

                return new OkObjectResult(new PaginatedResponse<TripPoint>()
                {
                    Total = total,
                    Count = points.Length,
                    Start = start,
                    Items = points
                });
            }
        }
    }
}