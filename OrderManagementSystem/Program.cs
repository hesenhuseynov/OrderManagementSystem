using Asp.Versioning;
using FluentValidation;
using OrderManagementSystem.Features.Customers.GetAllCustomer;
using OrderManagementSystem.Features.Customers.GetById;
using OrderManagementSystem.Features.Orders.Cancel;
using OrderManagementSystem.Features.Orders.Create;
using OrderManagementSystem.Features.Orders.GetById;
using OrderManagementSystem.Features.Orders.Pay;
using OrderManagementSystem.Features.Products.Create;
using OrderManagementSystem.Features.Products.Search;
using OrderManagementSystem.Infrastructure;
using OrderManagementSystem.Middleware;
using System.Reflection;

namespace OrderManagementSystem
{
    public partial class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddScoped<GetAllCustomersHandler>();
            builder.Services.AddScoped<GetCustomerByIdHandler>();
            builder.Services.AddScoped<CreateOrderHandler>();
            builder.Services.AddScoped<GetOrderByIdHandler>();
            builder.Services.AddScoped<CancelOrderHandler>();
            builder.Services.AddScoped<CreateProductHandler>();
            builder.Services.AddScoped<SearchProductsHandler>();
            builder.Services.AddScoped<PayOrderHandler>();

            builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            builder.Services.AddInfrastructure(builder.Configuration);

            builder.Services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
            }).AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

            var app = builder.Build();

            app.UseGlobalExcpetionMiddleware();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            var apiVersionSet = app.NewApiVersionSet()
                .HasApiVersion(new ApiVersion(1, 0))
                .ReportApiVersions()
                .Build();

            var customers = app.MapGroup("/api/v{version:apiVersion}/customers")
                .WithApiVersionSet(apiVersionSet)
                .WithTags("Customers");

            customers
                .MapGetAllCustomersEndpoint()
                .MapGetCustomerByIdEndpoint();

            var orders = app.MapGroup("/api/v{version:apiVersion}/orders")
                .WithApiVersionSet(apiVersionSet)
                .WithTags("Orders");

            orders
                .MapCreateOrderEndpoint()
                .MapGetOrderByIdEndpoint()
                .MapCancelOrderEndpoint()
                .MapPayOrderEndpoint();

            var products = app.MapGroup("/api/v{version:apiVersion}/products")
                .WithApiVersionSet(apiVersionSet)
                .WithTags("Products");

            products
                .MapCreateProductEndpoint()
                .MapSearchProductsEndpoint();

            app.Run();
        }
    }
}
