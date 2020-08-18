using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CouchbaseWebAppExample.Models;
using Microsoft.AspNetCore.Mvc;

namespace CouchbaseWebAppExample.Controllers
{
    [Route("/trace")]
    [Controller]
    public class TraceViewerController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("{activityId}")]
        public IActionResult Get(string activityId)
        {
            if (ActivityNode.ParentChildRelationships.TryGetValue(activityId, out var node))
            {
                return View(node);
            }

            return NotFound();
        }
    }
}
