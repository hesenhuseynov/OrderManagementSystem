using Microsoft.AspNetCore.Diagnostics;
using OrderManagementSystem.Common.Errors;

namespace OrderManagementSystem.Features.Orders
{
    public static class OrderErrors
    {
        public static Error CustomerNotFound(int customerId) =>
            Error.NotFound(
                "order.customer_not_found",
                $"Customer with id '{customerId}' was not found'"
                );

        public static Error ProductNotFound(int productId) =>
            Error.NotFound(
                "order.product_not_found",
                $"Product  with id '{productId}'  was not found "
                );

        public static Error DuplicateProducts =>
            Error.Conflict(
                "order.duplicate_products",
                 $"Duplicate products  entries are not allowed in the same order"
                );

        public static Error InsufficientStock(int producId) =>
            Error.Conflict(
                "order.insufficient_stock",
                $"Insufficient stock for product'{producId}'. "
                );

        public static Error OrderNotFound(int orderId) =>
            Error.NotFound(
                "order.not_found",
                $"Order with id  '{orderId}' was  not found"
                );
         
        public static Error InvalidStatusForCancellation(string currentStatus) =>
            Error.Conflict(
                 "order.invalid_status_for_cancellation",
                 $"Order cannot be  cancelled  when status is {currentStatus}"
                );

        public static Error OrderItemsNotFound(int orderId) =>
            Error.NotFound("order.items.not_found",
                 $"Order items for order '{orderId}'   were not found "
                );
    }
}
