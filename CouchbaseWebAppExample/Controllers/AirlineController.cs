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
    public sealed class AirlineController : Controller
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
            using var span =
                DiagListener.StartActivity(new Activity(nameof(AirlineController) + "." + nameof(Index)), null);
            var bucket = await _travelSample.GetBucketAsync().ConfigureAwait(false);
            var queryResults = await bucket.Cluster
                .QueryAsync<dynamic>("SELECT icao, name FROM `travel-sample` WHERE type = 'airline' ORDER BY name")
                .ConfigureAwait(false);
            var results = new List<(string icao, string name)>();
            await foreach (var qr in queryResults.Rows)
            {
                results.Add((qr.icao, qr.name));
            }
            
            DiagListener.StopActivity(span, null);
            return View(results);
        }

        [HttpGet("{icao}")]
        public async Task<IActionResult> Get(string icao)
        {
            using var span = 
                DiagListener.StartActivity(new Activity(nameof(AirlineController) + "." + nameof(Get)), new { icao });
            Airline airline;
            var membucket = await _membucket.GetBucketAsync().ConfigureAwait(false);
            var memCollection = membucket.DefaultCollection();
            var travelSampleBucket = await _travelSample.GetBucketAsync().ConfigureAwait(false);
            var collection = travelSampleBucket.DefaultCollection();
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

                    return View(airline);
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

            return View(airline);
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
