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

We provide extended test samples using Postgres + EF Core + TestContainers and basic samples with a Cosmos emulator.

See: `EntityInjector.Samples.PostgresTest` and `EntityInjector.Samples.CosmosTest`

## Development

- Requires .NET 8 SDK
- Tests use docker containers

## Roadmap

- Fetching data from Property
