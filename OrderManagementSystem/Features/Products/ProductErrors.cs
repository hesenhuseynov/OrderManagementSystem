using OrderManagementSystem.Common.Errors;

namespace OrderManagementSystem.Features.Products
{
    public static class ProductErrors
    {
        public static Error DuplicateSku(string sku) =>
            Error.Conflict("product.duplicate_sku", $"Product with SKU  '{sku}' already exists");
    }
}
