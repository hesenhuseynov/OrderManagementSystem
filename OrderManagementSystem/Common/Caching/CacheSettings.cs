
namespace OrderManagementSystem.Common.Caching
{
    public sealed class CacheSettings
    {

        public const string SectionName = "Cache";

        public int OrderByIdTtlMinutes { get; init; } = 15;

        public int OperationTimeoutMilliseconds { get; init; } = 500;

        public double CircuitFailureRatio { get; init; } = 0.5;

        public int CircuitMinimumThroughput { get; init; } = 8;

        public int CircuitSamplingDurationSeconds { get; init; } = 10;

        public int CircuitBreakDurationSeconds { get; init; } = 15;
    }
}
