
using System;
using HealthChecks.UI.Configuration;
using HealthChecks.UI.Core;
using HealthChecks.UI.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Builder
{
    public static class EndpointRouteBuilderExtensions
    {
        public static IEndpointConventionBuilder MapHealthChecksUI(this IEndpointRouteBuilder builder,
            Action<Options> setupOptions = null)
        {
            var options = new Options();
            setupOptions?.Invoke(options);
            
            EnsureValidApiOptions(options);

            var apiDelegate =
                builder.CreateApplicationBuilder()
                    .UseMiddleware<UIApiEndpointMiddleware>()
                    .Build();

            var webhooksDelegate =
                builder.CreateApplicationBuilder()
                    .UseMiddleware<UIWebHooksApiMiddleware>()
                    .Build();
 
            
            var embeddedResourcesAssembly = typeof(UIResource).Assembly;
            new UIEndpointsResourceMapper(new UIEmbeddedResourcesReader(embeddedResourcesAssembly))
                .Map(builder, options);
            
            builder.MapGet(options.ApiPath, apiDelegate);
            return builder.MapGet(options.WebhookPath, webhooksDelegate);
            

//           
//
//            var embeddedResourcesAssembly = typeof(UIResource).Assembly;
//
//            app.Map(options.ApiPath, appBuilder => appBuilder.UseMiddleware<UIApiEndpointMiddleware>());
//            app.Map(options.WebhookPath, appBuilder => appBuilder.UseMiddleware<UIWebHooksApiMiddleware>());
//
//            new UIResourcesMapper(
//                    new UIEmbeddedResourcesReader(embeddedResourcesAssembly))
//                .Map(app, options);
//
//            return app;


        }
        
        private static void EnsureValidApiOptions(Options options)
        {
            Action<string, string> ensureValidPath = (string path, string argument) =>
            {
                if (string.IsNullOrEmpty(path) || !path.StartsWith("/"))
                {
                    throw new ArgumentException("The value for customized path can't be null and need to start with / character.", argument);
                }
            };

            ensureValidPath(options.ApiPath, nameof(Options.ApiPath));
            ensureValidPath(options.UIPath, nameof(Options.UIPath));
            ensureValidPath(options.WebhookPath, nameof(Options.WebhookPath));
        }
    }
}