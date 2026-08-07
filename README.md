# SQL Stored Procedures CRUD Generator

An open-source SQL Server CRUD stored procedure generator with a desktop interface for
[`sp_CRUDGen`](https://github.com/kevinmartintech/sp_CRUDGen), the SQL Server
stored-procedure generator created by **Kevin Martin**.

> SQL Stored Procedures CRUD Generator builds a visual workflow around Kevin Martin's original
> `sp_CRUDGen` project. Please visit and support the
> [upstream repository](https://github.com/kevinmartintech/sp_CRUDGen).

SQL Stored Procedures CRUD Generator connects to a target SQL Server database, discovers tables and
views, lets you select the procedure types you need, and shows the generated SQL
before anything is changed.

## MVP features

- Windows and SQL authentication
- Table and view discovery with filtering
- Detection and installation of the bundled `sp_CRUDGen`
- All 11 generation switches exposed in the UI
- Safe preview mode (`@GenerateStoredProcedures = 0`)
- Explicit confirmation before installing or creating procedures
- SQL output viewer and clear connection/generation status
- No credential persistence or telemetry

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server 2016 or newer, or Azure SQL Database
- Permission to read database metadata
- Additional DDL permission when installing the generator or creating procedures

Install and run `sp_CRUDGen` in a user database, never in `master`.

## Run locally

```powershell
dotnet restore CrudGenerator.sln
dotnet run --project src/CrudGenerator.Desktop/CrudGenerator.Desktop.csproj
```

The default server is `(localdb)\MSSQLLocalDB`. Enter the database name before
connecting. SQL authentication passwords live only in the current process and
are never written to disk.

## How the safety flow works

1. **Generate safe preview** calls `sp_CRUDGen` with
   `@GenerateStoredProcedures = 0`.
2. **Install / update generator** and **Create procedures** require the
   **I confirm database changes** checkbox.
3. The confirmation resets after each database-changing action.

Always review generated SQL and test against a non-production database first.

## How SQL Stored Procedures CRUD Generator uses sp_CRUDGen

The application does not reimplement the generator in C#. It treats the
original T-SQL procedure as the generation engine:

1. The app connects directly to the SQL Server database selected by the user.
2. It reads `sys.objects` and `sys.schemas` to display user tables and views.
3. It checks `OBJECT_ID(N'dbo.sp_CRUDGen', N'P')` to determine whether the
   generator is installed in that database.
4. If requested, **Install / update generator** executes the bundled upstream
   [`sql/sp_CRUDGen.sql`](sql/sp_CRUDGen.sql) script in the selected user
   database. SQL Server `GO` batch separators are handled by the app.
5. For generation, the app calls `dbo.sp_CRUDGen` as a parameterized stored
   procedure. The selected object is passed as `schema.object`, and every UI
   checkbox maps to its corresponding `@Generate...` parameter.
6. Preview mode passes `@GenerateStoredProcedures = 0`; create mode passes
   `@GenerateStoredProcedures = 1` only after explicit confirmation.
7. Returned result sets and SQL Server informational messages are collected and
   shown in the SQL preview panel.

The generator remains installed and executed in the target database, as the
upstream project recommends. SQL Stored Procedures CRUD Generator never installs it in `master`.
For implementation details, see
[docs/SP_CRUDGEN_INTEGRATION.md](docs/SP_CRUDGEN_INTEGRATION.md).

## Project layout

```text
src/CrudGenerator.Core       Domain models and service contract
src/CrudGenerator.SqlServer  Parameterized SQL Server integration
src/CrudGenerator.Desktop    Avalonia UI and MVVM workflow
tests/CrudGenerator.Core.Tests
sql/sp_CRUDGen.sql            Upstream generator bundled for installation
```

## Build and test

```powershell
dotnet build CrudGenerator.sln
dotnet test CrudGenerator.sln
```

## License and attribution

SQL Stored Procedures CRUD Generator is available under the [MIT License](LICENSE). The bundled
`sp_CRUDGen.sql` retains its original copyright and MIT terms; see
[THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

Kevin Martin and Kevin Martin Tech, LLC retain copyright in the bundled
`sp_CRUDGen` source. This project is an independent community interface and is
not presented as an official release of the upstream project.

## Contributing

Contributions are welcome from everyone. Bug reports, accessibility fixes,
documentation, tests, database compatibility improvements, and UI ideas are all
valuable. Read [CONTRIBUTING.md](CONTRIBUTING.md), then open an issue or pull
request. By participating, you agree to follow our
[Code of Conduct](CODE_OF_CONDUCT.md).
