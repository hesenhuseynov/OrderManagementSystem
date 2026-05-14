namespace OrderManagementSystem.Infrastructure.Configuration
{
    public class DatabaseOptions
    {
        public const string SectionName="ConnectionStrings";
        public string DefaultConnection { get; init; } = String.Empty;
    }
}
