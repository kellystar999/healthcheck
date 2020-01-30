﻿using HealthChecks.UI.Image.Configuration;
using HealthChecks.UI.Image.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using HealthChecks.UI.Configuration;
using HealthChecks.UI.Core.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder
{
    public static class IEndpointRouteBuilderExtensions
    {
        public static IEndpointConventionBuilder MapHealthChecksUI(this IEndpointRouteBuilder builder,
            IConfiguration configuration)
        {
            builder.MapHealthCheckPushEndpoint(configuration);
            
            return builder.MapHealthChecksUI(setup =>
            {
                setup.ConfigureStylesheet(configuration);
                setup.ConfigurePaths(configuration);
                
            });
        }

        private static void MapHealthCheckPushEndpoint(this IEndpointRouteBuilder builder,
            IConfiguration configuration)
        {
            if (Boolean.TryParse(configuration["enable_push_endpoint"], out bool enabled))
            {
                if (!enabled) return;
                
                Console.WriteLine("HealthChecks Push Endpoint Enabled");
                
                builder.MapPost("/healthchecks/push", async context =>
                {
                    using var streamReader = new StreamReader(context.Request.Body);
                    var content = await streamReader.ReadToEndAsync();
                    
                    var endpoint = JsonDocument.Parse(content);
                    var root = endpoint.RootElement;
                    var name = root.GetProperty("name").GetString();
                    var uri = root.GetProperty("uri").GetString();
                    var type = root.GetProperty("type").GetInt16();

                    if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(uri))
                    {
                        using var scope = context.RequestServices.GetService<IServiceScopeFactory>().CreateScope();
                        var db = scope.ServiceProvider.GetService<HealthChecksDb>();
                        
                        await db.Configurations.AddAsync(new HealthCheckConfiguration
                        {
                            Name = name,
                            Uri = uri,
                            DiscoveryService = "kubernetes"
                        });
                        
                        await db.SaveChangesAsync();
                        
                        Console.WriteLine($"[Push] New service added: {name} - {uri}");
                    }
                });
            }
        }
    }
}