# Changelog

All notable changes to this project will be documented here. Releases follow
[Semantic Versioning](https://semver.org/).

## [Unreleased]

## [0.3.0] - 2026-08-07

### Added

- Single-table selection and generation using `@SchemaTableOrViewName`.
- Advanced forms for the upstream naming, audit, time, row-version, temporal,
  and search-separator parameters.
- Non-destructive generated-procedure validation with per-procedure results.
- Disposable LocalDB integration coverage for preview, create, browse, and test.

### Changed

- Table metadata is loaded for the selected database; views remain excluded.
- The generator procedure itself is excluded from the generated-procedure list.

## [0.2.0] - 2026-08-07

### Added

- Server-level discovery of every accessible online database.
- Automatic loading and filtering of stored procedures created by `sp_CRUDGen`.

### Changed

- Database selection now drives generator detection and procedure browsing.
- Preview and create actions use `sp_CRUDGen` database-wide mode without loading
  tables or views into the desktop UI.

## [0.1.0] - 2026-08-07

### Added

- Cross-platform Avalonia desktop interface.
- Windows and SQL Server authentication flows.
- SQL Server table and view discovery.
- Installation and detection of the upstream `sp_CRUDGen` procedure.
- All 11 `sp_CRUDGen` generation switches.
- Safe SQL preview and explicit confirmation for database changes.
- MIT licensing, upstream attribution, contribution templates, and CI.

[Unreleased]: https://github.com/Yoshuwa/sql-stored-procedures-crud-generator/compare/v0.3.0...HEAD
[0.3.0]: https://github.com/Yoshuwa/sql-stored-procedures-crud-generator/compare/v0.2.0...v0.3.0
[0.2.0]: https://github.com/Yoshuwa/sql-stored-procedures-crud-generator/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/Yoshuwa/sql-stored-procedures-crud-generator/releases/tag/v0.1.0
