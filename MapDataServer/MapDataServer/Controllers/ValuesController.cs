using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MapDataServer.Services;
using MapDataServer.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace MapDataServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private IDbClient DbClient { get; }
        public ValuesController(IDbClient dbClient)
        {
            DbClient = dbClient;
        }

        // GET api/values
        [HttpGet]
        public async Task<ActionResult<IEnumerable<string>>> Get()
        {
            await DbClient.Open();
            var sucess = await DbClient.CreateTable<Tuple<int, string, string, long>>("test_table",
                new DbRowParameter("id").MakeAutoIncrement().MakePrimaryKey(),
                new DbRowParameter("name").MakeNotNull(),
                new DbRowParameter("description"),
                new DbRowParameter("subId").BlockNotNull().MakePrimaryKey());
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
