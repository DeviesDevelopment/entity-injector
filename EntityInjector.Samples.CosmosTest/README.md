# EntityInjector.Samples.CosmosTest

This sample demonstrates how to use EntityInjector with a Cosmos database using the docker linux emulator.  
It shows how to bind route parameters directly to entities using dependency injection.

Note that due to issues with the cosmos emulator we have added a retry on each test.

## Prerequisites

- Docker installed and running
- .NET 8 SDK installed

## How to run

1. From the repository root, start the database using Docker Compose:

```bash
docker compose up --build
```

This will create a Cosmos container which can be easily browsed on localhost:1234.

2. From the sample project directory, build and run the tests:

```bash
dotnet clean && dotnet restore && dotnet test
```
