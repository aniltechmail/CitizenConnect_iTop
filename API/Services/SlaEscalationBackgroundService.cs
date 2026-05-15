using Core.Interfaces.Services;

namespace API.Services
{
    public class SlaEscalationBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SlaEscalationBackgroundService> _logger;
        private static readonly TimeSpan Interval = TimeSpan.FromMinutes(30);

        public SlaEscalationBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<SlaEscalationBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<IEscalationService>();
                    var created = await service.ScanAsync(stoppingToken);
                    if (created > 0)
                        _logger.LogInformation("SLA escalation scanner created {Count} escalation events.", created);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SLA escalation scanner failed.");
                }

                await Task.Delay(Interval, stoppingToken);
            }
        }
    }
}
