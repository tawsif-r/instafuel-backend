# dotnet Backend for Instafuel
## Table Of Content
- [Introduction](#introduction)
- [Choice of template for dotnet](#choice-of-template-for-dotnet)
- []()

Backend API for InstaFuel, a fuel marketplace platform connecting customers with fuel suppliers and delivery agents.

**Product Name**: Fuel On-Demand (Working Title)

- ASP.NET Core Web API
- .NET 10
- C#
- PostgreSQL 17
- Entity Framework Core
- Npgsql PostgreSQL provider
- Serilog
- OpenAPI and Swagger
- Docker and Docker Compose


Install the following tools:

- Git
- Docker Engine
- Docker Compose

**Core Objective**: Deliver a fast, safe, and seamless micro-delivery service for fuel, connecting customers with local suppliers and automated delivery riders while maintaining strict compliance and safety.

```bash
git --version
docker --version
docker compose version
```

The API runs inside Docker, so installing the .NET SDK locally is not required for the standard Docker workflow. The .NET 10 SDK is needed for local development outside Docker.

## Project Structure

```text
instafuel/
├── Instafuel.Api/
│   ├── Common/
│   ├── Controllers/
│   ├── Data/
│   │   └── Configurations/
│   ├── Middleware/
│   ├── Modules/
│   │   ├── Identity/
│   │   │   └── DTOs/
│   │   ├── Customer/
│   │   ├── Riders/
│   │   └── Suppliers/
│   ├── Models/
│   ├── Migrations/
│   ├── Program.cs
│   ├── Dockerfile
│   ├── appsettings.json
│   └── Instafuel.Api.csproj
├── docs/
│   └── database_schema.md
├── .dockerignore
├── .env.example
├── docker-compose.yml
├── ARCHITECTURE.md
└── README.md
```

## Configuration

Docker Compose reads local environment variables from a `.env` file in the repository root.

Create your local environment file:

```bash
cp .env.example .env
```

Open `.env` and configure the database credentials:

```env
POSTGRES_USER=tawsif
POSTGRES_PASSWORD=your_local_password
POSTGRES_DB=instafuel
POSTGRES_PORT=5432
ASPNETCORE_ENVIRONMENT=Development
JWT_SIGNING_KEY=replace_with_a_random_secret_at_least_32_bytes
```

Replace `your_local_password` with a password of your choice.
Replace `JWT_SIGNING_KEY` with a random secret of at least 32 bytes before
starting the API. You can generate one with `openssl rand -base64 48`.

For local development outside Docker, keep the connection string in .NET User
Secrets instead of a tracked settings file:

```bash
dotnet user-secrets set "ConnectionStrings:PostgresConnection" "Host=localhost;Port=5432;Database=instafuel;Username=your_user;Password=your_password" --project Instafuel.Api
dotnet user-secrets set "Jwt:SigningKey" "paste-a-random-secret-here" --project Instafuel.Api
```

**Security requirements:**

- Never commit `.env` to Git.
- Commit `.env.example` with placeholder values only.
- Do not put database passwords in tracked `appsettings*.json` files.
- Do not share connection strings or terminal output containing real credentials.

## Run the Application with Docker Compose

Run these commands from the repository root.

Build the API image and start the API and database:

```bash
docker compose up --build
```

Docker Compose starts PostgreSQL and waits for its health check before starting the API.

In the Development environment, the API applies pending Entity Framework Core migrations during startup.

The API listens on port `8080`.

### Access Swagger UI

Open the following URL in your browser:

```text
http://localhost:8080/swagger
```

OpenAPI JSON:

```text
http://localhost:8080/swagger/v1/swagger.json
```

Swagger UI lists the API endpoints implemented in the current project.

Implemented endpoints include:

- `POST /api/v1/auth/register`
- `POST /api/v1/auth/register/rider` (riders require admin verification)
- `POST /api/v1/auth/login`
- `POST /api/v1/auth/refresh`
- `POST /api/v1/auth/logout`
- `GET /api/v1/auth/me` (Bearer access token required)
- `POST /api/v1/supplier/applications` and `GET /api/v1/supplier/applications/mine`
- `GET /api/v1/admin/suppliers/pending` and `PUT /api/v1/admin/suppliers/{supplierId}/status`
- `GET/POST /api/v1/supplier/{supplierId}/members` (owner can add an existing account)
- `GET/POST /api/v1/supplier/{supplierId}/locations`, `PUT/DELETE .../locations/{locationId}`
- `GET /api/v1/supplier/{supplierId}/products`, `GET/POST .../offers`, `PUT .../offers/{offerId}`
- `GET /api/v1/supplier/{supplierId}/inventory` with stock receipt and adjustment endpoints
- `GET /api/v1/supplier/{supplierId}/orders` and `PUT .../orders/{supplierOrderId}/status`
- `GET/PUT /api/v1/rider/profile`
- `PUT /api/v1/rider/preferred-areas`
- `PUT /api/v1/rider/availability`
- `GET /api/v1/rider/jobs/available` and `POST /api/v1/rider/jobs/{deliveryId}/accept`
- `GET /api/v1/rider/deliveries` and `POST /api/v1/rider/deliveries/{deliveryId}/decline`
- `PUT /api/v1/rider/deliveries/{deliveryId}/status`
- `PUT /api/v1/rider/deliveries/{deliveryId}/location`
- `/hubs/delivery-tracking` (SignalR `JoinOrder` method)
- `GET /api/v1/admin/riders/pending` and `PUT /api/v1/admin/riders/{riderUserId}/verification`
- `GET /api/v1/admin/riders/failed-deliveries` and `POST /api/v1/admin/riders/failed-deliveries/{deliveryId}/retry`
- `GET /health/live`
- `GET /health/ready` (checks PostgreSQL connectivity)

Rider jobs are visible only to verified, online riders whose preferred cities
match a ready supplier order. Delivery completion requires a safety checklist,
proof of delivery, and consistent COD cash collection. Earnings remain pending
until commission and payout rules are defined.

Checkout requests must include a unique `IdempotencyKey` per intended order.
The API creates the COD payment intent in the same database transaction as the
order. Configure `DELIVERY_FEE_BDT` in each non-Development deployment;
the Development settings use a zero fee for local work only. The current pricing
is a flat fee, so configure the business-approved amount before accepting live
orders. Failed deliveries require a rider reason and can be reopened by an admin
for reassignment.

Development applies pending EF Core migrations at startup when a connection
string is configured. Production expects migrations to be applied by the
deployment process.

Customer registration accepts an email, a phone number, or both; login accepts
exactly one of email or phone. Email is stored lowercase, and phone formatting
spaces/punctuation are normalized before uniqueness checks. Registration/login
may include optional `deviceInfo` (up to 200 characters). Refresh tokens are
stored only as hashes, rotate on each refresh, and share a family identifier;
reusing a revoked token revokes all active tokens in that family. Applying the
auth family migration revokes pre-existing refresh tokens because their earlier
rotation relationships were not recorded, so existing sessions must sign in
again once after deployment.

Supplier applications are reviewed by admins, and each supplier route checks
the signed-in user's membership in the requested supplier account. Member
management currently adds an already registered account directly; outbound
email invitations and owner transfer need an email provider and additional
workflow. Supplier earnings and payouts remain unavailable until commission
and payout rules are defined.

For databases created before the snake_case naming convention was enabled,
run [the migration-history upgrade SQL](docs/upgrade_migration_history.sql)
once before using EF Core migration commands.

### Run in the Background

To start the services in detached mode:

```bash
docker compose up --build -d
```

Check service status:

```bash
docker compose ps
```

View API logs:

```bash
docker compose logs -f api
```

View database logs:

```bash
docker compose logs -f db
```

Stop the services:

```bash
docker compose down
```

The named PostgreSQL volume is preserved when using `docker compose down`. To remove the database volume and its stored data, a separate destructive action is required.

## Database and Migrations

PostgreSQL runs as a Docker Compose service. The API connects to it using the `PostgresConnection` connection string supplied through environment configuration.

Inside Docker Compose, the database hostname is `db`, not `localhost`.

In Development, pending migrations are applied automatically when the API starts.

To create a migration during local development, install the EF Core CLI if necessary:

```bash
dotnet tool install --global dotnet-ef
```

Create a migration after changing the EF Core model:

```bash
dotnet ef migrations add MigrationName --project Instafuel.Api
```

Review the generated migration before applying it to a database.

For a local database update outside the Docker startup workflow:

```bash
dotnet ef database update --project Instafuel.Api
```

Ensure the local environment has the correct connection string before running EF Core commands.

## Logging

The application uses Serilog for console logging and error-level file logging.

Log files are written under:

```text
Instafuel.Api/logs/
```

Avoid logging passwords, tokens, connection strings, or other sensitive information.

## Documentation

- [Architecture](ARCHITECTURE.md)
- [Database Schema](docs/database_schema.md)
