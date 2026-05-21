# Order Management System

Order Management System is a backend-focused ASP.NET Core project built to practice and demonstrate real-world backend concepts such as order processing, payment handling, caching, background processing, and integration testing.

The project follows a feature-based structure and uses Dapper with SQL Server instead of Entity Framework. The main goal is to keep the code explicit, understandable, and close to how backend systems behave in production-like scenarios.

## Features

- Customer, product, and order management
- Order creation with stock validation
- Order cancellation with stock restoration
- Payment flow with idempotency support
- Fake payment provider for testing payment scenarios
- Outbox Pattern for reliable background event processing
- Elasticsearch indexing for product search
- Redis cache-aside usage for order reads
- Integration tests using Testcontainers
- ProblemDetails-based error handling
- FluentValidation-based request validation

## Tech Stack

- ASP.NET Core
- Dapper
- SQL Server
- Redis
- Elasticsearch
- Testcontainers
- FluentValidation
- xUnit
- Docker

## Architecture Notes

The project is organized using a feature-based structure. Each feature contains its own request, response, validator, handler, and endpoint where applicable.

Some of the backend patterns and concepts used in the project:

- Result Pattern
- Vertical Slice style organization
- Outbox Pattern
- Idempotency for payments
- Cache-aside strategy
- Background processing
- SQL transaction handling
- Atomic status updates
- Integration testing with real containers

## Payment Flow

The payment module currently uses a fake payment gateway. This allows the system to test payment success and failure scenarios without depending on a real payment provider.

The payment flow includes:

- Idempotency key validation
- Payment record creation
- Order status transition from `Pending` to `Paid`
- Payment status tracking
- Order status history
- `PaymentCompleted` outbox event creation

This part of the project is intentionally designed to be extendable for real providers such as Stripe or local payment gateways in the future.

## Outbox Pattern

The project uses the Outbox Pattern to avoid direct coupling between the main database transaction and external systems.

For example, when a product is created:

1. Product data is saved to SQL Server.
2. A `ProductCreated` event is saved to the `OutboxEvents` table in the same transaction.
3. A background processor later reads the event and indexes the product into Elasticsearch.

This helps avoid the common dual-write problem between the database and external systems.

## Elasticsearch

Products are indexed into Elasticsearch through the outbox processor.

Current indexed product fields:

- ProductId
- SKU
- ProductName
- Price
- IsActive

Search functionality is planned to be improved further.

## Integration Tests

The integration test project uses Testcontainers to run SQL Server and Redis in isolated containers.

The tests cover important flows such as:

- Creating orders
- Cancelling orders
- Product creation with outbox event creation
- Payment success flow
- Payment idempotency behavior

Run tests:

```bash
dotnet test
