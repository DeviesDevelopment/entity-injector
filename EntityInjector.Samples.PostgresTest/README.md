# EntityInjector.Samples.PostgresTest

This sample demonstrates how to use EntityInjector with a Postgres database using Entity Framework Core and TestContainers.  
It shows how to bind route parameters directly to entities using dependency injection.

## Prerequisites

- Docker installed and running
- .NET 8 SDK installed

## How to run

1. From the repository root, start the database using Docker Compose:

```bash
docker compose up --build
```

This will create a Postgres container and run `init.sql` to seed the database with test data.

2. From the sample project directory, build and run the tests:

```bash
dotnet clean && dotnet restore && dotnet test
```
