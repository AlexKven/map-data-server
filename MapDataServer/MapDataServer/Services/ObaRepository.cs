using LinqToDB;
using MapDataServer.Helpers;
using MapDataServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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
    }
}
