using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Couchbase;
using Couchbase.Extensions.DependencyInjection;

namespace CouchbaseWebAppExample.Buckets
{
    public interface ITravelSample : INamedBucketProvider { }
    public interface IMembucket : INamedBucketProvider { }
}
