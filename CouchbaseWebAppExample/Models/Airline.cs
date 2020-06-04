using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CouchbaseWebAppExample.Models
{
    public class Airline
    {
        private static readonly string TypeName = nameof(Airline).ToLowerInvariant();

        public string Type => TypeName;
        public string Callsign { get; set; }
        public string Country { get; set; }
        public string Iata { get; set; }
        public string Icao { get; set; }
        public long Id { get; set; }
        public string Name { get; set; }
    }
}
