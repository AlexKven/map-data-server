using MapDataServer.Helpers;
using MapDataServer.Models.OneBusAway;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace MapDataServer.Services
{
    public class ObaApi
    {
        const string API_STOPS_FOR_LOCATION = "https://api.pugetsound.onebusaway.org/api/where/stops-for-location.xml";
        const string API_ARRIVALS_AND_DEPARTURES_FOR_STOP = "https://api.pugetsound.onebusaway.org/api/where/arrivals-and-departures-for-stop/{0}.xml";
        const string API_ARRIVAL_AND_DEPARTURE_FOR_STOP = "https://api.pugetsound.onebusaway.org/api/where/arrival-and-departure-for-stop/{0}.xml";
        const string SCHEDULE_FOR_STOP = "https://api.pugetsound.onebusaway.org/api/where/schedule-for-stop/{0}.xml";
        const string STOPS_FOR_ROUTE = "https://api.pugetsound.onebusaway.org/api/where/stops-for-route/{0}.xml";
        const string ROUTE = "https://api.pugetsound.onebusaway.org/api/where/route/{0}.xml";
        const string TRIP = "https://api.pugetsound.onebusaway.org/api/where/trip/{0}.xml";
        const string SHAPE = "https://api.pugetsound.onebusaway.org/api/where/shape/{0}.xml";
        const string TRIP_DETAILS = "https://api.pugetsound.onebusaway.org/api/where/trip-details/{0}.xml";

        private IHttpClientFactory HttpClientFactory { get; }

        private HttpClient HttpClient => HttpClientFactory.CreateClient();
        protected IConfiguration Configuration { get; }

        public ObaApi(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            Configuration = configuration;
            HttpClientFactory = httpClientFactory;
        }

        public async Task<RouteResponse> GetRoute(string id, CancellationToken cancellationToken)
        {
            var urlBuilder = new StringBuilder(string.Format(ROUTE, id));
            urlBuilder.Append($"?key={Configuration["obaKey"]}");
            urlBuilder.Append("&includeReferences=false");
            var request = new HttpRequestMessage(HttpMethod.Get, urlBuilder.ToString());
            var response = await HttpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;
            try
            {
                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    var content = await new StreamReader(stream).ReadToEndAsync();

                    using (var mem = new MemoryStream())
                    {
                        XmlDocument xDoc = new XmlDocument();
                        xDoc.LoadXml(content);
                        return RouteResponse.FromXml(xDoc.FirstChild);
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                    throw;
                return null;
            }
        }

        public async Task<TripResponse> GetTrip(string id, CancellationToken cancellationToken)
        {
            var urlBuilder = new StringBuilder(string.Format(TRIP, id));
            urlBuilder.Append($"?key={Configuration["obaKey"]}");
            urlBuilder.Append("&includeReferences=false");
            var request = new HttpRequestMessage(HttpMethod.Get, urlBuilder.ToString());
            var response = await HttpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;
            try
            {
                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    var content = await new StreamReader(stream).ReadToEndAsync();

                    using (var mem = new MemoryStream())
                    {
                        XmlDocument xDoc = new XmlDocument();
                        xDoc.LoadXml(content);
                        return TripResponse.FromXml(xDoc.FirstChild);
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                    throw;
                return null;
            }
        }

        public async Task<TripDetailsResponse> GetTripDetails(string id, CancellationToken cancellationToken,
            DateTime? date = null, bool includeSchedule = true, bool includeStatus = true, long? time = null)
        {
            var urlBuilder = new StringBuilder(string.Format(TRIP_DETAILS, id));
            urlBuilder.Append($"?key={Configuration["obaKey"]}");
            if (date.TryUse(out var d))
                urlBuilder.Append($"&date={d.Year.ToString("D4")}-{d.Month.ToString("D2")}-{d.Day.ToString("D2")}");
            if (time.TryUse(out var timeVal))
                urlBuilder.Append($"&time={timeVal}");
            urlBuilder.Append($"&includeSchedule={includeSchedule}");
            urlBuilder.Append($"&includeSchedule={includeStatus}");
            urlBuilder.Append("&includeTrip=false");
            urlBuilder.Append("&includeReferences=false");
            var request = new HttpRequestMessage(HttpMethod.Get, urlBuilder.ToString());
            var response = await HttpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;
            try
            {
                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    var content = await new StreamReader(stream).ReadToEndAsync();

                    using (var mem = new MemoryStream())
                    {
                        XmlDocument xDoc = new XmlDocument();
                        xDoc.LoadXml(content);
                        return TripDetailsResponse.FromXml(xDoc.FirstChild);
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                    throw;
                return null;
            }
        }

        public async Task<ShapeResponse> GetShape(string id, CancellationToken cancellationToken)
        {
            var urlBuilder = new StringBuilder(string.Format(SHAPE, id));
            urlBuilder.Append($"?key={Configuration["obaKey"]}");
            urlBuilder.Append("&includeReferences=false");
            var request = new HttpRequestMessage(HttpMethod.Get, urlBuilder.ToString());
            var response = await HttpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;
            try
            {
                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    var content = await new StreamReader(stream).ReadToEndAsync();

                    using (var mem = new MemoryStream())
                    {
                        XmlDocument xDoc = new XmlDocument();
                        xDoc.LoadXml(content);
                        return ShapeResponse.FromXml(xDoc.FirstChild);
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                    throw;
                return null;
            }
        }

        public void Dispose()
        {
            HttpClient.Dispose();
        }
    }
}
