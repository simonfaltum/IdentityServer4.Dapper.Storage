using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using IdentityServer4.Dapper.Storage.DataLayer;
using IdentityServer4.Dapper.Storage.Options;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityServer4.Dapper.Storage.Services
{
    internal class TokenCleanup
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly OperationalStoreOptions _options;

        private CancellationTokenSource _source;

        public TimeSpan CleanupInterval => TimeSpan.FromSeconds(_options.TokenCleanupInterval);

        public TokenCleanup(IServiceProvider serviceProvider, OperationalStoreOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            if (_options.TokenCleanupInterval < 1) throw new ArgumentException("Token cleanup interval must be at least 1 second");
            if (_options.TokenCleanupBatchSize < 1) throw new ArgumentException("Token cleanup batch size interval must be at least 1");

            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public void Start()
        {
            Start(CancellationToken.None);
        }

        public void Start(CancellationToken cancellationToken)
        {
            if (_source != null) throw new InvalidOperationException("Already started. Call Stop first.");

            _source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            Task.Factory.StartNew(() => StartInternal(_source.Token), cancellationToken);
        }

        public void Stop()
        {
            //if (_source == null) throw new InvalidOperationException("Not started. Call Start first.");

            if(_source != null) { 
            _source.Cancel();
            _source = null;
            }
        }

        private async Task StartInternal(CancellationToken cancellationToken)
        {
            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Debug.WriteLine("CancellationRequested. Exiting.");
                    break;
                }

                try
                {
                    await Task.Delay(CleanupInterval, cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    Debug.WriteLine("TaskCanceledException. Exiting.");
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Task.Delay exception: {0}. Exiting.", ex.Message);
                    break;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    Debug.WriteLine("CancellationRequested. Exiting.");
                    break;
                }

                await ClearTokens();
            }
        }

        public async Task ClearTokens()
        {
            try
            {
                Debug.WriteLine("Querying for tokens to clear");

                var found = _options.TokenCleanupBatchSize;

                using var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
                var store = serviceScope.ServiceProvider.GetService<IPersistedGrantProvider>();
                var timestamp = DateTimeOffset.UtcNow;
                do
                {
                    found = await store.QueryExpired(timestamp);
                    Debug.WriteLine($"Clearing {found} tokens");

                    if (found > 0)
                    {
                        try
                        {
                            await store.RemoveRange(timestamp);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("Concurrency exception clearing tokens: {exception}", ex.Message);
                            throw; //throw out to stop while loop
                        }
                    }
                }
                while (found > 0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception clearing tokens: {ex.Message}");
            }
        }
    }
}
