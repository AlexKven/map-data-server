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

        public async Task<ObaServicePeriod> GetCurrentServicePeriod(string agencyId, DateTime now)
        {
            await Database.Initializer;
            var random = new Random();
            ObaServicePeriod result = null;
            using (var results = (from period in Database.ObaServicePeriods
                                  where period.ObaAgencyId == agencyId &&
                                  period.EndTime != null
                                  orderby period.EndTime descending
                                  select period)
                          .ToAsyncEnumerable().GetEnumerator())
            {
                // Check latest service period that applies to current time
                while (await results.MoveNext(CancellationToken.None))
                {
                    if (results.Current.EndTime > now)
                    {
                        result = results.Current;
                        break;
                    }
                }
            }

            // If nothing, check if there is a null end time period
            if (result == null)
            {
                result = await (from period in Database.ObaServicePeriods
                                where period.ObaAgencyId == agencyId &&
                                period.EndTime == null
                                select period).ToAsyncEnumerable().FirstOrDefault();
            }

            // Still nothing, then time to create a new one
            if (result == null)
            {
                result = new ObaServicePeriod() { ObaAgencyId = agencyId, Id = random.RandomLong() };
                await Database.InsertOrReplaceAsync(result);
            }
            return result;
        }

        public async Task<IEnumerable<ObaTripStopLink>> GetStopsForTrip(string tripId, DateTime now)
        {
            var agencyId = tripId.ParseAgencyId();
            var servicePeriod = await GetCurrentServicePeriod(agencyId, now);
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

        public async Task<ObaRoute> GetRoute(string routeId, DateTime now)
        {
            var agencyId = routeId.ParseAgencyId();
            var servicePeriod = await GetCurrentServicePeriod(agencyId, now);
            var result = await (from route in Database.ObaRoutes
                where route.ObaRouteId == routeId &&
                route.ObaServicePeriodId == servicePeriod.Id
                select route).ToAsyncEnumerable().FirstOrDefault();
            if (result != null)
                return result;

            var obaRoute = (await ObaApi.GetRoute(routeId, CancellationToken.None)).Data;
            var dbRoute = new ObaRoute()
            {
                Color = obaRoute.Color,
                Description = obaRoute.Description,
                LongName = obaRoute.LongName,
                ObaRouteId = obaRoute.Id,
                ObaServicePeriodId = servicePeriod.Id,
                ShortName = obaRoute.ShortName,
                TextColor = obaRoute.TextColor,
                Type = obaRoute.Type,
                Url = obaRoute.Url
            };
            await Database.InsertOrReplaceAsync(dbRoute);
            return dbRoute;
        }
    }
}
