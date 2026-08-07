# How the application integrates with sp_CRUDGen

SQL Stored Procedures CRUD Generator is a user interface around the original
[`sp_CRUDGen`](https://github.com/kevinmartintech/sp_CRUDGen) stored procedure
created by Kevin Martin. The C# application coordinates installation,
configuration, preview, and execution; the upstream T-SQL remains the code
generation engine.

## Components

- `sql/sp_CRUDGen.sql` is a bundled snapshot of the upstream installer.
- `CrudGenerator.Core` defines database objects, generation options, and the
  database-service contract.
- `CrudGenerator.SqlServer` connects with `Microsoft.Data.SqlClient`, reads
  metadata, installs the SQL script, and invokes `dbo.sp_CRUDGen`.
- `CrudGenerator.Desktop` maps the user's choices to that service and presents
  output and status.

## Connection and discovery

The user supplies a SQL Server instance and user database. Credentials are kept
in memory for the lifetime of the process and are not persisted. After opening
the connection, the application queries `sys.objects` joined to `sys.schemas`
for non-system tables (`U`) and views (`V`).

The app checks for the generator with:

```sql
SELECT CASE
    WHEN OBJECT_ID(N'dbo.sp_CRUDGen', N'P') IS NULL THEN 0
    ELSE 1
END;
```

## Installation

The **Install / update generator** action reads the bundled SQL file, separates
the script on standalone `GO` lines, and executes each batch in order against
the selected database. The user must first select **I confirm database
changes**. Installation should never target `master`.

The bundled file is upstream code and retains its original MIT license and
copyright. See `THIRD_PARTY_NOTICES.md`.

## Generation call

Generation uses `CommandType.StoredProcedure` and calls `dbo.sp_CRUDGen` with
SQL parameters. The UI options map directly as follows:

| UI option | sp_CRUDGen parameter |
| --- | --- |
| Create | `@GenerateCreate` |
| Create multiple | `@GenerateCreateMultiple` |
| Read | `@GenerateRead` |
| Read eager | `@GenerateReadEager` |
| Update | `@GenerateUpdate` |
| Update multiple | `@GenerateUpdateMultiple` |
| Upsert | `@GenerateUpsert` |
| Indate | `@GenerateIndate` |
| Delete | `@GenerateDelete` |
| Delete multiple | `@GenerateDeleteMultiple` |
| Search | `@GenerateSearch` |

The selected object is passed to `@SchemaTableOrViewName` in `schema.object`
format.

### Preview

Preview calls the procedure with:

```sql
@GenerateStoredProcedures = 0
```

The generator produces the T-SQL without installing the generated CRUD
procedures. The app reads all result sets and informational messages into the
preview panel.

### Create procedures

After the user confirms database changes, create mode calls with:

```sql
@GenerateStoredProcedures = 1
```

The generator then creates or regenerates the selected procedure types using
its own upstream behavior. The confirmation is cleared after the operation.

## Upstream updates

Generator improvements should normally be contributed to the
[upstream project](https://github.com/kevinmartintech/sp_CRUDGen) first. When
syncing a new `sp_CRUDGen.sql` snapshot here:

1. Review the upstream diff and parameter signature.
2. Preserve Kevin Martin Tech, LLC's copyright and MIT terms.
3. Update the application mapping if parameters changed.
4. Test preview and create modes against a disposable SQL Server database.
5. Record the upstream source in the pull request.
