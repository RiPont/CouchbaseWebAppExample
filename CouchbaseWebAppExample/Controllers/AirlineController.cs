using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Couchbase;
using Couchbase.Core.Exceptions.KeyValue;
using Couchbase.Extensions.DependencyInjection;
using Couchbase.KeyValue;
using CouchbaseWebAppExample.Buckets;
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
        private readonly IClusterProvider _clusterProvider;
        private readonly ITravelSample _travelSample;
        private readonly IMembucket _membucket;
        private const string BucketName = @"travel-sample";

        private static DiagnosticListener DiagListener = new DiagnosticListener("AirlineController");

        public AirlineController(
            IBucketProvider bucketProvider, 
            IClusterProvider clusterProvider, 
            ITravelSample travelSample, 
            IMembucket membucket)
        {
            _bucketProvider = bucketProvider;
            _clusterProvider = clusterProvider;
            _travelSample = travelSample;
            _membucket = membucket;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            await Task.Delay(0);
            return Ok(new {Hello = "World"});
        }

        [HttpGet("{icao}/{subdoc?}", Name = "Get")]
        public async Task<IActionResult> Get(string icao, string subdoc = null)
        {
            var activity = new Activity("AirlineController.Get");
            using var span = DiagListener.StartActivity(activity, new {icao, subdoc});
            Airline airline;
            var membucket = await _membucket.GetBucketAsync().ConfigureAwait(false);
            var memCollection = membucket.DefaultCollection();
            var travelSampleBucket = await _travelSample.GetBucketAsync().ConfigureAwait(false);
            var collection = travelSampleBucket.DefaultCollection();
            if (int.TryParse(icao, out var id))
            {
                var fullId = $"airline_{id}";
                if (!string.IsNullOrWhiteSpace(subdoc))
                {
                    var lookupInResult = await collection.LookupInAsync(fullId, specs =>
                        specs.Get("$document.CAS", true)
                            .Get("$document.exptime", true)
                             .Get(subdoc, false)
                             .GetFull());
                    var result0 = lookupInResult.ContentAs<dynamic>(0);
                    var result1 = lookupInResult.ContentAs<string>(1);
                    var wrapper = new
                    {
                        path = subdoc, 
                        result0 = (object)result0,
                        result1 = (object)result1,
                        result2 = (object)lookupInResult.ContentAs<string>(2),
                            airline = lookupInResult.ContentAs<Airline>(3)
                    };

                    return Content(JObject.FromObject(wrapper).ToString(), "application/json");
                }

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

                    return Ok(airline);
                }
                catch (DocumentNotFoundException)
                {
                    var cluster = await _clusterProvider.GetClusterAsync().ConfigureAwait(false);
                    var queryResult = await cluster.QueryAsync<Airline>(
                        "SELECT `travel-sample`.* FROM `travel-sample` WHERE type='airline' AND icao = $icao",
                        options => options.Parameter("icao", icao)).ConfigureAwait(false);
                    airline = await queryResult.Rows.FirstOrDefaultAsync().ConfigureAwait(false);
                }
            }

            if (airline == null)
            {
                return NotFound();
            }

            if (airline?.Icao != null)
            {
                var memUpsertResult = await memCollection.UpsertAsync<Airline>(airline.Icao, airline, options => options.Expiry(TimeSpan.FromSeconds(30)));
            }

            return Ok(airline);
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
