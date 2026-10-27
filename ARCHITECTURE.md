# InstaFuel Backend Architecture

## 1. Overview

InstaFuel is an on-demand fuel delivery platform that connects customers, fuel suppliers, and delivery riders through a centralized backend API.

The backend is built using:

- **ASP.NET Core Web API**
- **.NET 10**
- **C#**
- **Entity Framework Core**
- **PostgreSQL**
- **Npgsql**
- **Docker and Docker Compose**
- **Serilog** for application logging

The backend follows a **Modular Monolith Architecture**.

The application is organized into logical business modules while remaining a single deployable backend application.

Modules share the same application process and database. They communicate through application services and internal method calls rather than inter-service HTTP requests.

## 2. Architecture Principles

The backend follows these principles:

1. Controllers handle HTTP requests and responses.
2. Business logic belongs in services, not controllers.
3. Request and response DTOs define the API contract.
4. Database entities are never returned directly from API endpoints.
5. Entity Framework Core manages database access.
6. FluentValidation validates incoming request DTOs.
7. ProblemDetails provides standardized API error responses.
8. Authorization ensures users can access only permitted resources.
9. Each business module owns its related application functionality.
10. Configuration and secrets must be managed outside source code.

## 3. Logical Business Modules

### 3.1 Identity

Responsible for authentication and identity-related operations.

Responsibilities:

- User registration
- Login and logout
- JWT access and refresh tokens
- Phone OTP verification
- Password reset
- Authentication-related validation

### 3.2 Accounts

Responsible for user profiles and account-related resources.

Responsibilities:

- Customer profiles
- Customer addresses
- Customer vehicles
- Rider profiles and verification
- Supplier accounts and membership
- Account deactivation

### 3.3 Orders

Responsible for customer orders and the order lifecycle.

Responsibilities:

- Price quotation
- Order creation
- Order details
- Customer order history
- Order cancellation
- Order status management
- Order status history

### 3.4 Inventory

Responsible for products, offers, stock, and inventory operations.

Responsibilities:

- Product and category management
- Supplier offers
- Supplier inventory balances
- Stock receipts and adjustments
- Stock reservations
- Stock release and commitment
- Low-stock alerts

### 3.5 Delivery

Responsible for delivery assignment and rider operations.

Responsibilities:

- Rider availability
- Preferred delivery areas
- Delivery assignment
- Delivery status updates
- Proof of delivery
- Rider location tracking
- Fuel safety and pickup checklist

### 3.6 Payments

Responsible for payment and financial operations.

Responsibilities:

- Cash on Delivery
- Online payment integration
- Payment records
- Payment reconciliation
- Refunds
- Supplier commissions and payouts
- Invoices and receipts

Notifications, reviews, promotions, administration, and reporting may be implemented as supporting modules or organized within the relevant business modules as the project evolves.

## 4. Project Folder Structure

The following is the intended organization of the backend as the project develops. Existing files may be migrated into this structure incrementally.

```text
instafuel/
├── Instafuel.Api/
│   ├── Controllers/
│   │
│   ├── Modules/
│   │   ├── Identity/
│   │   │   ├── Controllers/
│   │   │   ├── DTOs/
│   │   │   ├── Services/
│   │   │   └── Validators/
│   │   │
│   │   ├── Accounts/
│   │   │   ├── Controllers/
│   │   │   ├── DTOs/
│   │   │   ├── Services/
│   │   │   └── Validators/
│   │   │
│   │   ├── Orders/
│   │   │   ├── Controllers/
│   │   │   ├── DTOs/
│   │   │   ├── Services/
│   │   │   └── Validators/
│   │   │
│   │   ├── Inventory/
│   │   │   ├── Controllers/
│   │   │   ├── DTOs/
│   │   │   ├── Services/
│   │   │   └── Validators/
│   │   │
│   │   ├── Delivery/
│   │   │   ├── Controllers/
│   │   │   ├── DTOs/
│   │   │   ├── Services/
│   │   │   └── Validators/
│   │   │
│   │   └── Payments/
│   │       ├── Controllers/
│   │       ├── DTOs/
│   │       ├── Services/
│   │       └── Validators/
│   │
│   ├── Data/
│   │   └── Configurations/
│   │
│   ├── Models/
│   │
│   ├── Exceptions/
│   ├── Handlers/
│   ├── Middleware/
│   ├── Migrations/
│   ├── Program.cs
│   ├── appsettings.json
│   └── Instafuel.Api.csproj
│
├── Instafuel.Api.Tests/
├── docs/
├── ARCHITECTURE.md
├── CONTRIBUTING.md
├── docker-compose.yml
└── README.md
```

### Folder Responsibilities

| Folder | Responsibility |
|---|---|
| `Controllers/` | HTTP routing, request handling, and response generation |
| `Modules/` | Business functionality grouped by domain |
| `DTOs/` | API request and response contracts |
| `Services/` | Business rules and application workflows |
| `Validators/` | FluentValidation rules for request DTOs |
| `Data/` | Database context and data-access configuration |
| `Data/Configurations/` | EF Core entity mappings and database constraints |
| `Models/` | Database entity classes |
| `Exceptions/` | Custom application exceptions |
| `Handlers/` | Global exception handling and standardized errors |
| `Middleware/` | Cross-cutting HTTP request processing |
| `Migrations/` | EF Core database migrations |
| `Instafuel.Api.Tests/` | Automated tests |

The module-based structure is the target organization, not a requirement to move every existing file immediately. Changes should be incremental and must preserve existing functionality.

## 5. HTTP Request Flow

A typical API request follows this flow:

```text
Client Application
       |
       v
HTTP Request
       |
       v
ASP.NET Core Middleware
       |
       +--> Request ID
       |
       +--> Exception Handling
       |
       +--> Request Logging
       |
       v
Controller
       |
       v
Request DTO Validation
       |
       v
Application Service
       |
       v
Entity Framework Core
       |
       v
PostgreSQL Database
       |
       v
Application Service
       |
       v
Response DTO
       |
       v
HTTP Response
```

### Step-by-Step Explanation

**Step 1: Client sends a request**

A customer, rider, supplier, or administrative application sends an HTTP request to the backend API.

**Step 2: Middleware processes the request**

ASP.NET Core middleware performs cross-cutting operations such as request ID handling, exception handling, and request logging.

**Step 3: Controller receives the request**

The controller identifies the endpoint, reads route parameters and request data, and invokes the relevant application service.

**Step 4: Request validation runs**

FluentValidation checks incoming request DTOs against the applicable validation rules.

Invalid requests return a standardized validation error response.

**Step 5: Service executes business logic**

The service performs the required application operations and enforces business rules.

For example, the order service checks order requirements and coordinates stock reservation and order creation.

**Step 6: Database operations execute**

Entity Framework Core interacts with PostgreSQL through the configured database context.

**Step 7: Response DTO is returned**

The application maps the result to a response DTO. The controller returns the appropriate HTTP status code and response body.

Database entities must not be exposed directly through API responses.

## 6. API Conventions

### 6.1 API Versioning

Versioned endpoints use the `/api/v1` route prefix.

Examples:

```text
/api/v1/accounts/me
/api/v1/accounts/addresses
/api/v1/orders
/api/v1/orders/{id}
/api/v1/inventory/offers
/api/v1/payments
```

The route prefix defines the API version convention. The application must consistently apply the same convention to feature endpoints.

### 6.2 Request and Response DTOs

Every endpoint must use appropriate request and response DTOs.

Example:

```text
CreateAddressRequest
AddressResponse
```

Request DTOs define the data accepted by an endpoint.

Response DTOs define the data returned to clients.

Database entities must not be returned directly.

### 6.3 Validation

FluentValidation defines request validation rules.

Typical validation requirements include:

- Required fields
- String length restrictions
- Valid identifiers
- Positive quantities
- Valid address and contact information
- Permitted values for request fields

Validation failures should produce a consistent ProblemDetails response compatible with the global exception handling implementation.

### 6.4 Pagination

List endpoints that support pagination should use a consistent response format.

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "total": 0
}
```

Definitions:

- `items`: Records returned for the requested page.
- `page`: Current page number.
- `pageSize`: Maximum number of records requested per page.
- `total`: Total number of matching records before pagination.

Pagination should validate page parameters and apply database-level filtering and pagination rather than loading every record into memory.

Not every endpoint requires pagination. It is primarily intended for collection and search endpoints.

### 6.5 Error Responses

API errors should follow the ProblemDetails format.

The global exception handler maps known application exceptions to appropriate HTTP status codes and returns a request ID for troubleshooting.

Validation errors should include useful field-level error details without exposing internal implementation information.

## 7. Business Logic and Controller Responsibilities

Controllers should remain focused on HTTP concerns.

Controllers should:

- Accept HTTP requests.
- Bind route, query, and body parameters.
- Invoke validation and application services through the established integration.
- Return appropriate HTTP status codes.
- Return DTOs rather than database entities.

Services should:

- Enforce business rules.
- Coordinate database operations.
- Handle application workflows.
- Coordinate related operations where necessary.
- Return application results for the controller to map into response DTOs.

Controllers should not contain substantial business logic or directly implement complex inventory, order, or payment workflows.

## 8. Database and Persistence

PostgreSQL is the primary relational database.

Entity Framework Core provides object-relational mapping and database access through Npgsql.

Database responsibilities include:

- Entity relationships
- Foreign keys
- Unique constraints
- Check constraints
- Indexes
- Transaction management
- Database migrations

EF Core mappings are maintained through entity configuration classes under `Data/Configurations/`.

Schema changes should be implemented through migrations and reviewed before being applied to an environment.

Financial and historical records require appropriate referential integrity and deletion behavior.

## 9. Authentication and Authorization

Authentication establishes the identity of the caller.

Authorization determines whether the authenticated caller can perform a requested operation.

The backend must apply appropriate access controls to protected resources.

Examples include:

- Customers accessing their own addresses and orders.
- Riders accessing their own profiles and assigned deliveries.
- Suppliers accessing resources belonging to their supplier account.
- Administrators performing authorized management operations.

Authorization must be enforced by the backend rather than relying only on client-side restrictions.

## 10. Logging and Error Handling

Serilog is used for application logging.

The backend includes request ID middleware and a global exception handler.

The request ID helps correlate HTTP requests with log entries and error responses.

Unexpected exceptions should be logged appropriately, while client-facing responses must not reveal sensitive implementation details.

Known application errors should map to meaningful HTTP status codes.

## 11. Configuration and Secrets

Application configuration is provided through ASP.NET Core configuration.

Environment-specific values should be supplied through appropriate configuration sources.

Secrets such as database passwords, JWT signing keys, and payment-provider credentials must not be committed to source control.

Docker Compose can be used for local development and container orchestration.

Production configuration should be managed separately from development configuration.

## 12. Deployment Overview

The backend is packaged as an ASP.NET Core application and can run in a Docker container.

A typical deployment consists of:

```text
Client Applications
       |
       v
HTTPS / Reverse Proxy
       |
       v
InstaFuel ASP.NET Core API
       |
       v
PostgreSQL Database
```

The API and database may run in separate containers or on separate servers, depending on the deployment environment.

The application communicates with PostgreSQL through its configured connection string and network access rules.

Production deployment should consider:

- HTTPS and TLS
- Secure secret management
- Database backups and recovery
- Health checks
- Application logging and monitoring
- Database migration strategy
- Access controls and network security

## 13. Testing

The backend should include automated tests for important application behavior.

Testing should cover:

- Request DTO validation
- Controller HTTP behavior
- Service business rules
- Authentication and authorization
- Database interactions
- Order and inventory workflows
- Error handling and ProblemDetails
- Pagination behavior

Integration tests should verify interactions with the database where appropriate.

A successful build alone does not establish that an endpoint behaves correctly.

## 14. Contribution Guidelines

Before adding a new endpoint:

1. Place it in the appropriate business module.
2. Use the `/api/v1` route convention.
3. Define request and response DTOs.
4. Add FluentValidation rules where required.
5. Keep business logic in application services.
6. Return DTOs rather than database entities.
7. Use the standard paged response for paginated list endpoints.
8. Apply appropriate authentication and authorization.
9. Use the established error-handling conventions.
10. Add or update relevant automated tests.
11. Update API documentation when necessary.

## 15. Related Documentation

- `README.md` — Project setup and general instructions.
- `CONTRIBUTING.md` — Development conventions and contribution workflow.
- `docs/database_schema.md` — Database schema documentation.
- `docker-compose.yml` — Local container configuration.

---

**Architecture Summary:** InstaFuel uses a single ASP.NET Core Web API application organized as a modular monolith, with clear separation between HTTP controllers, DTOs, validation, application services, and PostgreSQL persistence.