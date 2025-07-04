# EntityInjector

EntityInjector is a set of libraries designed to simplify extracting entities from (primarily) databases
in a clean and dependency-injected way.

👉 This repository contains multiple NuGet packages:

- `EntityInjector.Route` — Route binding to entities (via HTTP context)
- `EntityInjector.Property` — (coming soon)

## Packages

| Package              | NuGet                                                                                                                | Description                                         |
| -------------------- | -------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------- |
| EntityInjector.Route | [![NuGet](https://img.shields.io/nuget/v/EntityInjector.Route)](https://www.nuget.org/packages/EntityInjector.Route) | Bind route parameters directly to database entities |

## Samples

We provide samples using Postgres + EF Core + TestContainers.
See: `EntityInjector.Samples.PostgresTest`

## Development

- Requires .NET 8 SDK
- Tests use docker containers

## Roadmap

- Add Cosmos DB sample
- Extend tests + failure scenarios
- Exception handling

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
