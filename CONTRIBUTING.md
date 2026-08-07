# Contributing

Thanks for helping improve SQL Stored Procedures CRUD Generator.

## Development workflow

1. Open an issue before starting a large behavioral or UI change.
2. Create a focused branch from `develop` and keep unrelated changes out of the pull request.
3. Run `dotnet build CrudGenerator.sln` and `dotnet test CrudGenerator.sln`.
4. Include tests for changes to generation options, validation, or SQL behavior.
5. Never include real connection strings, credentials, or customer schema data.

Use `feature/*`, `fix/*`, `docs/*`, or `chore/*` branch names and target
`develop` for ordinary pull requests. See
[docs/BRANCHING_AND_RELEASES.md](docs/BRANCHING_AND_RELEASES.md).

## Database safety

- Test DDL behavior against a disposable or non-production database.
- Keep preview mode non-mutating.
- Require an explicit user action for every database-changing workflow.
- Use SQL parameters for values; do not assemble commands from user input.
- Do not install `sp_CRUDGen` in `master`.

## Upstream SQL

Changes to `sql/sp_CRUDGen.sql` should normally be contributed to the upstream
[`kevinmartintech/sp_CRUDGen`](https://github.com/kevinmartintech/sp_CRUDGen)
project first. Preserve its copyright and license notice when syncing updates.
