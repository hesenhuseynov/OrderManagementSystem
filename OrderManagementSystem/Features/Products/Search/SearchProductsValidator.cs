using FluentValidation;

namespace OrderManagementSystem.Features.Products.Search
{
    public sealed class SearchProductsValidator:AbstractValidator<SearchProductsRequest>
    {
        public SearchProductsValidator()
        {
            RuleFor(x => x.Query)
               .NotEmpty()
               .WithMessage("Search query is reuired")
               .WithErrorCode("product.search.query_required")
               .MaximumLength(100)
               .WithMessage("Search query must not exceed 100 characters")
               .WithErrorCode("product.search.query_too_long");
        }
    }
}
