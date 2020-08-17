using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Couchbase.Extensions.DependencyInjection;
using CouchbaseWebAppExample.Buckets;
using Microsoft.AspNetCore.Mvc;

namespace CouchbaseWebAppExample.Controllers
{
    [Route("/")]
    [Controller]
    public class HomeController : Controller
    {
        private readonly IBucketProvider _bucketProvider;
        private readonly IClusterProvider _clusterProvider;
        private readonly ITravelSample _travelSample;
        private readonly IMembucket _membucket;

        public HomeController(IBucketProvider bucketProvider,
            IClusterProvider clusterProvider,
            ITravelSample travelSample,
            IMembucket membucket)
        {
            _bucketProvider = bucketProvider;
            _clusterProvider = clusterProvider;
            _travelSample = travelSample;
            _membucket = membucket;
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
