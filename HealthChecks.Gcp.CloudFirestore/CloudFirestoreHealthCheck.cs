﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthChecks.Gcp.CloudFirestore
{
    public class CloudFirestoreHealthCheck : IHealthCheck
    {
        private readonly CloudFirestoreOptions _cloudFirestoreOptions = new CloudFirestoreOptions();
        public CloudFirestoreHealthCheck(CloudFirestoreOptions cloudFirestoreOptions)
        {
            _cloudFirestoreOptions = cloudFirestoreOptions;
        }
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var currentRootCollections = await GetRootCollectionsAsync(cancellationToken);

                if (_cloudFirestoreOptions.RequiredCollections != null)
                {
                    var inexistantCollections = _cloudFirestoreOptions.RequiredCollections
                        .Except(currentRootCollections);
                    
                    if (inexistantCollections.Any())
                    {
                        return new HealthCheckResult(
                            context.Registration.FailureStatus,
                            description: "Collections not found: " + string.Join(", ", "'" + inexistantCollections + "'")
                        );
                    }
                }

                return HealthCheckResult.Healthy();
            }
            catch (Exception ex)
            {
                return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
            }
        }

        private async Task<List<string>> GetRootCollectionsAsync(CancellationToken cancellationToken)
        {
            var collections = new List<string>();
            var enumerator = _cloudFirestoreOptions.FirestoreDatabase.ListRootCollectionsAsync().GetEnumerator();

            while (await enumerator.MoveNext(cancellationToken))
            {
                collections.Add(enumerator.Current.Id);
            }

            return collections;
        }
    }
}
