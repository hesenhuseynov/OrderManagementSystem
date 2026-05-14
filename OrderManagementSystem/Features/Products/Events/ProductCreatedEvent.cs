using System.IO.Pipes;
using System.Security.Cryptography.X509Certificates;

namespace OrderManagementSystem.Features.Products.Events
{
    public sealed record ProductCreatedEvent(int ProductId, string Sku, string ProductName, decimal Price, bool IsActive);
}
