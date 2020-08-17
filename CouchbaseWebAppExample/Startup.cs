#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Couchbase.Core.Diagnostics.Tracing;
using Couchbase.Extensions.DependencyInjection;
using CouchbaseWebAppExample.Buckets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenTelemetry.Trace;

namespace CouchbaseWebAppExample
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging();

            var activitySource = new ActivitySource(RequestTracing.SourceName);

            services.AddCouchbase(opts =>
                opts.WithConnectionString("couchbase://windrunner")
                    .WithCredentials("Administrator", "password")
                    .WithLogging()

                    // Use a threshold of 0 to make sure there are samples for over-threshold events.
                    .WithThresholdTracing(opts => opts.WithKvThreshold(TimeSpan.Zero))

                    .IgnoreRemoteCertificateNameMismatch = true);
            services.AddCouchbaseBucket<ITravelSample>("travel-sample");
            services.AddCouchbaseBucket<IMembucket>("membucket");
            services.AddDistributedMemoryCache();
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime applicationLifetime)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                ////endpoints.MapGet("/", async context =>
                ////{
                ////    context.Response.ContentType = "text/html";
                ////    await context.Response.WriteAsync("<a href='api/airline/10'>Test GET</a>");
                ////});

                endpoints.MapControllers();
            });

            applicationLifetime.ApplicationStopped.Register(async () =>
            {
                await app.ApplicationServices.GetRequiredService<ICouchbaseLifetimeService>().CloseAsync()
                    .ConfigureAwait(false);
            });

            ActivityRequestTracer.Subscribe((activity, kvp) => {
                Console.WriteLine($"Couchbase.Activity::{kvp.Key} ({activity?.DisplayName})");
                if (kvp.Key.EndsWith(".Stop") && activity != null)
                {
                    var ids = new Stack<string>();
                    var node = activity;
                    while (node != null && ids.Count < 100)
                    {
                        ids.Push((node.Id ?? "NO_ID") + $"({node.OperationName})");
                        node = node.Parent;
                    }

                    var idPath = string.Join("->", ids);

                    Console.WriteLine(idPath + ":" + JsonConvert.SerializeObject(ThresholdSummary.FromActivity(activity)));
                }
            });
        }
    }
}
