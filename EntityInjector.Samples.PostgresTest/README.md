# Entity Injector Samples Postgres Test

To run this, follow these steps:

## Prerequisites:

- Working docker installation
- .NET 8 installed

## Steps:

- In the repo root run `docker compose up --build`

This will create a postgres database and run init.sql to seed the database with test data.

- In this project's root run `dotnet clean && dotnet restore && dotnet test`