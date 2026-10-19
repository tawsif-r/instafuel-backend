# InstaFuel Backend

Backend API for Fuel On-Demand (InstaFuel), a marketplace platform that connects customers with fuel suppliers and delivery agents.

## Technology Stack

- ASP.NET Core Web API
- .NET 10
- C#
- PostgreSQL
- Entity Framework Core
- Npgsql PostgreSQL provider
- Serilog
- OpenAPI and Swagger

## Prerequisites

Install the following tools before running the project:

- .NET 10 SDK
- Docker Engine
- Docker Compose
- Git

Verify the installations:

```bash
dotnet --version
docker --version
docker compose version
git --version
```

## Project Structure

```text
instafuel/
├── Instafuel.Api/
│   ├── Controllers/
│   ├── Models/
│   ├── Migrations/
│   ├── Properties/
│   ├── Program.cs
│   └── Instafuel.Api.csproj
├── docs/
│   └── database_schema.md
├── docker-compose.yml
└── README.md
```

## Database Setup

The project uses PostgreSQL, running locally through Docker Compose.

Start the database from the repository root:

```bash
docker compose up -d db
```

Check the container status:

```bash
docker compose ps
```

The current Compose configuration uses:

- Database: `instafuel`
- Username: `tawsif`
- Port: `5432`

Configure the `PostgresConnection` connection string in `Instafuel.Api/appsettings.json` to match your local database credentials.

Do not commit production credentials or secrets to source control.

## Database Migrations

Run the following commands from the repository root.

Restore project dependencies:

```bash
dotnet restore Instafuel.Api
```

Install the Entity Framework Core CLI tool if it is not already available:

```bash
dotnet tool install --global dotnet-ef
```

If the tool is already installed, update it when needed:

```bash
dotnet tool update --global dotnet-ef
```

Apply existing migrations to the configured database:

```bash
dotnet ef database update --project Instafuel.Api
```

Create a migration after changing the EF Core models:

```bash
dotnet ef migrations add MigrationName --project Instafuel.Api
```

Review generated migrations before applying them to a database.

## Run the Application

Start PostgreSQL first, then run the API:

```bash
dotnet watch --project Instafuel.Api
```

The application uses the connection string named `PostgresConnection`.

In the Development environment, OpenAPI and Swagger UI are enabled. Use the local URL printed by ASP.NET Core when the application starts.

## Logging

The application uses Serilog for console logging and error-level file logging.

Log files are written under:

```text
Instafuel.Api/logs/
```

Avoid logging passwords, credentials, tokens, or other sensitive information.

## Documentation

- [Database Schema](docs/database_schema.md)
- [Architecture](ARCHITECTURE.md)