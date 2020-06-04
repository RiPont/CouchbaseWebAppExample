using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Couchbase;
using Couchbase.Core.Exceptions.KeyValue;
using Couchbase.Extensions.DependencyInjection;
using Couchbase.KeyValue;
using CouchbaseWebAppExample.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace CouchbaseWebAppExample.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public sealed class AirlineController : ControllerBase
    {
        private readonly IBucketProvider _bucketProvider;
        private const string BucketName = @"travel-sample";

        public AirlineController(IBucketProvider bucketProvider)
        {
            _bucketProvider = bucketProvider;
        }

        // GET: api/TravelSample
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET: api/TravelSample/5
        [HttpGet("{icao}", Name = "Get")]
        public async Task<Airline> Get(string icao)
        {
            Airline airline;
            var bucket = await _bucketProvider.GetBucketAsync(BucketName).ConfigureAwait(false);
            var collection = bucket.DefaultCollection();
            var membucket = await _bucketProvider.GetBucketAsync("membucket");
            var memCollection = membucket.DefaultCollection();
            if (int.TryParse(icao, out var id))
            {
                var fullId = $"airline_{id}";
                var getResult = await collection.GetAsync(fullId).ConfigureAwait(false);
                airline = getResult.ContentAs<Airline>();
            }
            else
            {
                try
                {
                    var memGetResult = await memCollection.GetAsync(icao).ConfigureAwait(false);
                    airline = memGetResult.ContentAs<Airline>();
                    if (airline.Name == null)
                    {
                        throw new DocumentNotFoundException();
                    }

                    return airline;
                }
                catch (DocumentNotFoundException e)
                {
                    var queryResult = await bucket.Cluster.QueryAsync<Airline>(
                        "SELECT `travel-sample`.* FROM `travel-sample` WHERE type='airline' AND icao = $icao",
                        options => options.Parameter("icao", icao)).ConfigureAwait(false);
                    airline = await queryResult.Rows.FirstOrDefaultAsync().ConfigureAwait(false);
                }
            }

            if (airline?.Icao != null)
            {
                var memUpsertResult = await memCollection.UpsertAsync<Airline>(airline.Icao, airline, options => options.Expiry(TimeSpan.FromSeconds(30)));
            }

            return airline;
        }

        // POST: api/TravelSample
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT: api/TravelSample/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE: api/ApiWithActions/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
