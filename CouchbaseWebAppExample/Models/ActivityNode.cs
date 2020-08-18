using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace CouchbaseWebAppExample.Models
{
    public class ActivityNode
    {
        public static ConcurrentDictionary<string, ActivityNode> ParentChildRelationships =
            new ConcurrentDictionary<string, ActivityNode>();

        private Activity Activity { get; set; }

        public string Id => Activity.Id;
        public string OperationName => Activity.OperationName;
        public string DisplayName => Activity.DisplayName;

        public DateTimeOffset StartTimeUtc => Activity.StartTimeUtc;
        public TimeSpan Duration => Activity.Duration;

        public long DurationUs => (long) (Duration.TotalMilliseconds * 1000);

        public IEnumerable<KeyValuePair<string, string>> Tags => GetUniqueTags();

        public ConcurrentDictionary<string, ActivityNode> Children { get; } = new ConcurrentDictionary<string, ActivityNode>();

        private IEnumerable<KeyValuePair<string, string>> GetUniqueTags()
        {
            var uniqueTags = new Dictionary<string, string>();
            foreach (var kvp in Activity.Tags)
            {
                uniqueTags[kvp.Key] = kvp.Value;
            }

            return uniqueTags.OrderBy(kvp => kvp.Key);
        }

        public static void RecordStopActivity(Activity activity)
        {
            var thisNode = ParentChildRelationships.GetOrAdd(activity.Id, 
                a => new ActivityNode() { Activity = activity });

            var sanity = 0;
            while (thisNode.Activity.Parent != null 
                   && thisNode.Activity.Parent != thisNode.Activity
                   && sanity++ < 1000)
            {
                var thisNodeParent = thisNode.Activity.Parent;
                var parent =
                    ParentChildRelationships.GetOrAdd(thisNode.Activity.Parent.Id,
                        a => new ActivityNode() { Activity = thisNodeParent });

                parent.Children.GetOrAdd(thisNode.Activity.Id, thisNode);

                thisNode = parent;
            }
        }
    }
}
