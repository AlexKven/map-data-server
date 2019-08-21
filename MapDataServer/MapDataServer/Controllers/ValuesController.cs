using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using LinqToDB;
using MapDataServer.Models;
using MapDataServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using OsmSharp.Streams;
using OsmSharp.Tags;

namespace MapDataServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : BaseController
    {
        private IDatabase Database { get; }
        private IHttpClientFactory HttpClientFactory { get; }
        private IMapDownloader MapDownloader { get; }
        public ValuesController(IDatabase database, IHttpClientFactory httpClientFactory, IMapDownloader mapDownloader, IConfiguration configuration)
            : base(configuration)
        {
            Database = database;
            HttpClientFactory = httpClientFactory;
            MapDownloader = mapDownloader;
        }

        // GET api/values
        [HttpGet]
        public async Task<ActionResult<IEnumerable<string>>> Get()
        {
            //await Database.Initializer;
            //await MapDownloader.DownloadMapRegions(-12245, 4728, 22, 15);

            //var wayFinder = new RouteFinder(Database);
            //var node = await Database.MapNodes.Where(n => n.Id == 267814842).FirstAsync();
            //var way = await Database.MapWays.Where(w => w.Id == 12193812).FirstAsync();
            //var next = await wayFinder.FindNextStep(node, way);

            //await wayFinder.Test();

            //await Database.Initializer;
            //var httpClient = HttpClientFactory.CreateClient();

            //var request = new HttpRequestMessage(HttpMethod.Get, "https://api.openstreetmap.org/api/0.6/map?bbox=-122.366622,47.297579,-122.331534,47.306842");
            //var result = await httpClient.SendAsync(request);

            //using (result)
            //{
            //    using (var stream = await result.Content.ReadAsStreamAsync())
            //    {
            //        var source = new XmlOsmStreamSource(stream);
            //        var nodes = source.Where(geo => geo.Type == OsmSharp.OsmGeoType.Node).Cast<OsmSharp.Node>();
            //        foreach (var node in nodes)
            //        {
            //            if (!(node.Id.HasValue && node.Latitude.HasValue && node.Longitude.HasValue))
            //                continue;
            //            var dbNode = new MapNode()
            //            {
            //                Id = node.Id.Value,
            //                GeneratedDate = node.TimeStamp,
            //                SavedDate = DateTime.UtcNow,
            //                IsVisible = node.Visible,
            //                Latitude = node.Latitude.Value,
            //                Longitude = node.Longitude.Value
            //            };
            //            await Database.InsertOrReplaceAsync(dbNode);
            //            foreach (var tag in node.Tags ?? Enumerable.Empty<Tag>())
            //            {
            //                var dbTag = new GeoTag()
            //                {
            //                    GeoId = node.Id.Value,
            //                    Key = tag.Key,
            //                    Value = tag.Value
            //                };
            //                await Database.InsertOrReplaceAsync(dbTag);
            //            }
            //        }
            //    }
            //}

            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public ActionResult<string> Get(int id)
        {
            return "value";
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
