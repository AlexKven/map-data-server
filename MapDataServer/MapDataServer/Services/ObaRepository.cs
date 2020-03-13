using LinqToDB;
using MapDataServer.Helpers;
using MapDataServer.Models;
using MapDataServer.Models.OneBusAway;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MapDataServer.Services
{
    public class ObaRepository
    {
        private IDatabase Database { get; }
        private ObaApi ObaApi { get; }

        public ObaRepository(IDatabase database, ObaApi obaApi)
        {
            Database = database;
            ObaApi = obaApi;
        }

        public async Task<ObaServicePeriod> GetCurrentServicePeriod(string agencyId)
        {
            await Database.Initializer;
            var now = DateTime.UtcNow;
            var random = new Random();
            var result = await (from period in Database.ObaServicePeriods
                          where period.ObaAgencyId == agencyId
                          orderby period.EndTime descending select period)
                          .ToAsyncEnumerable().FirstOrDefault();
            if (result == null || (result.EndTime.HasValue && result.EndTime.Value < now))
            {
                result = new ObaServicePeriod() { ObaAgencyId = agencyId, Id = random.RandomLong() };
                await Database.InsertOrReplaceAsync(result);
            }
            return result;
        }

        public async Task<IEnumerable<ObaTripStopLink>> GetStopsForTrip(string tripId)
        {
            var agencyId = tripId.ParseAgencyId();
            var servicePeriod = await GetCurrentServicePeriod(agencyId);
            if (!(await Database.ObaTrips.AnyAsync(t =>
                t.ObaServicePeriodId == servicePeriod.Id &&
                t.ObaTripId == tripId)))
                await DownloadTrip(tripId, servicePeriod);

            return await (from link in Database.ObaTripStopLinks
                    where link.ObaTripId == tripId &&
                    link.ObaServicePeriodId == servicePeriod.Id
                    orderby link.StopSequence ascending
                    select link).ToAsyncEnumerable().ToArray();
        }

        private async Task DownloadTrip(string tripId, ObaServicePeriod servicePeriod)
        {
            var obaTrip = (await ObaApi.GetTrip(tripId, CancellationToken.None)).Data;
            var obaTripDetails = (await ObaApi.GetTripDetails(tripId, CancellationToken.None)).Data;
            EncodedPolyline obaShape = null;
            if (obaTrip.ShapeId != null)
                obaShape = (await ObaApi.GetShape(obaTrip.ShapeId, CancellationToken.None)).Data;

            var dbTrip = new ObaTrip()
            {
                ObaRouteId = obaTrip.RouteId,
                ObaServicePeriodId = servicePeriod.Id,
                ObaTripId = obaTrip.Id,
                ServiceId = obaTrip.ServiceId,
                Shape = obaShape?.Points,
                ShapeLength = obaShape?.Length ?? 0,
                TripHeadsign = obaTrip.TripHeadsign,
                TripShortName = obaTrip.TripShortName,
                ServiceDate = TimeHelpers.FromEpochMilliseconds(obaTripDetails.ServiceDate),
                TimeZone = obaTripDetails.Schedule.TimeZone
            };

            ushort index = 0;
            foreach (var stopTime in obaTripDetails.Schedule.StopTimes)
            {
                var stopLink = new ObaTripStopLink()
                {
                    ArrivalTime = stopTime.ArrivalTime,
                    DepartureTime = stopTime.DepartureTime,
                    DistanceAlongTrip = stopTime.DistanceAlongTrip,
                    ObaServicePeriodId = servicePeriod.Id,
                    ObaStopId = stopTime.StopId,
                    ObaTripId = obaTrip.Id,
                    StopSequence = index
                };
                await Database.InsertOrReplaceAsync(stopLink);
                index++;
            }
            await Database.InsertOrReplaceAsync(dbTrip);
        }
    }
}
