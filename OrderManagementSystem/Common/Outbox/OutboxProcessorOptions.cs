namespace OrderManagementSystem.Common.Outbox
{
    public sealed  class OutboxProcessorOptions
    {
        public const string SectionName = "OutboxProcessor";

        public bool Enabled { get; init; } = false;

        public int BatchSize { get; init; } = 20;

        public int PollingIntervalSeconds { get; init; } = 5;

        public int LockTimeoutSeconds { get; init ; } = 60;

        public int MaxRetryCount { get; init; } = 5; 
    }
}
