using ParsonsPuzzleApp.Interfaces;

namespace ParsonsPuzzleApp.Services
{
    /// <summary>
    /// Background service that periodically cleans up expired LTI state records.
    /// </summary>
    public class LtiStateCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<LtiStateCleanupService> _logger;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1);

        public LtiStateCleanupService(
            IServiceProvider serviceProvider,
            ILogger<LtiStateCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("LTI State Cleanup Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupExpiredStatesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during LTI state cleanup");
                }

                await Task.Delay(_cleanupInterval, stoppingToken);
            }

            _logger.LogInformation("LTI State Cleanup Service stopped");
        }

        private async Task CleanupExpiredStatesAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var ltiService = scope.ServiceProvider.GetRequiredService<ILtiService>();

            await ltiService.CleanupExpiredStatesAsync(cancellationToken);
        }
    }
}
