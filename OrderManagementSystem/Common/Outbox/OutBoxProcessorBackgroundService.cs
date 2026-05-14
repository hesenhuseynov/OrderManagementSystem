using Microsoft.Extensions.Options;

namespace OrderManagementSystem.Common.Outbox
{
    public sealed class OutboxProcessorBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly OutboxProcessorOptions _options;
        private readonly ILogger<OutboxProcessorBackgroundService> _logger;

        public OutboxProcessorBackgroundService(
            IServiceScopeFactory scopeFactory,
            IOptions<OutboxProcessorOptions> options,
            ILogger<OutboxProcessorBackgroundService> logger)
        {
            ArgumentNullException.ThrowIfNull(scopeFactory);
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(logger);

            _scopeFactory = scopeFactory;
            _options = options.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Enabled)
            {
                _logger.LogInformation("Outbox processor is disabled.");
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessBatchAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Outbox processor failed while processing batch.");
                }

                await Task.Delay(
                    TimeSpan.FromSeconds(_options.PollingIntervalSeconds),
                    stoppingToken);
            }
        }

        private async Task ProcessBatchAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();

            var outboxStore = scope.ServiceProvider.GetRequiredService<IOutboxStore>();
            var eventProcessor = scope.ServiceProvider.GetRequiredService<IOutboxEventProcessor>();

            var lockId = Guid.NewGuid();

            var messages = await outboxStore.ClaimBatchAsync(
                lockId,
                _options.BatchSize,
                _options.LockTimeoutSeconds,
                _options.MaxRetryCount,
                cancellationToken);

            foreach (var message in messages)
            {
                try
                {
                    await eventProcessor.ProcessAsync(message, cancellationToken);

                    await outboxStore.MarkProcessedAsync(
                        message.OutboxEventId,
                        lockId,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    await outboxStore.MarkFailedAsync(
                        message.OutboxEventId,
                        lockId,
                        ex.Message,
                        cancellationToken);

                    _logger.LogWarning(
                        ex,
                        "Outbox message processing failed. OutboxEventId: {OutboxEventId}, EventType: {EventType}",
                        message.OutboxEventId,
                        message.EventType);
                }
            }
        }
    }
}

 
