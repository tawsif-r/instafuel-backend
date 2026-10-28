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
│   │   └── Identity/
│   │       └── DTOs/
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
- `POST /api/v1/auth/login`
- `POST /api/v1/auth/refresh`
- `POST /api/v1/auth/logout`
- `GET /api/v1/auth/me` (Bearer access token required)
- `GET /health/live`
- `GET /health/ready` (checks PostgreSQL connectivity)

Development applies pending EF Core migrations at startup when a connection
string is configured. Production expects migrations to be applied by the
deployment process.

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
