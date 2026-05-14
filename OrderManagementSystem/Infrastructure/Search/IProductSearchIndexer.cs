namespace OrderManagementSystem.Infrastructure.Search
{
    public interface IProductSearchIndexer
    {
        Task IndexAsync(
            ProductSearchDocument document,
            CancellationToken cancellationToken);
    }
}
