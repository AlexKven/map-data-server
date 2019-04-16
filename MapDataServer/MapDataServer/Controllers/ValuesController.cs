using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MapDataServer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MapDataServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        public ValuesController(Models.TestContext context)
        {
            //context.Add(new TestModel1());
            //context.SaveChanges();
            AmazonSetup();
        }

        public async void AmazonSetup()
        {
            Amazon.Athena.AmazonAthenaClient client = new Amazon.Athena.AmazonAthenaClient(
                new Amazon.Runtime.BasicAWSCredentials("comeonwhatisit", "dontyouwanttoknow"),
                Amazon.RegionEndpoint.USEast2);
            var queries = await client.ListNamedQueriesAsync(new Amazon.Athena.Model.ListNamedQueriesRequest() { });
            var query = await client.GetNamedQueryAsync(new Amazon.Athena.Model.GetNamedQueryRequest() { NamedQueryId = "746fa110-45e7-47f9-9a12-3c8d0bb582ab" });
        }

        // GET api/values
        [HttpGet]
        public ActionResult<IEnumerable<string>> Get()
        {
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
