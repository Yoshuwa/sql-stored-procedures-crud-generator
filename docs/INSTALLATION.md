# Installation

Download the archive for your platform from the
[GitHub Releases page](https://github.com/Yoshuwa/sql-stored-procedures-crud-generator/releases)
and verify it with the published `SHA256SUMS.txt` file.

## Windows x64

1. Download the `win-x64.zip` archive.
2. Extract the entire archive to a writable folder.
3. Run `SqlStoredProceduresCrudGenerator.exe`.

## Linux x64

1. Download the `linux-x64.tar.gz` archive and extract it.
2. Make the application executable if your archive tool did not preserve the
   mode: `chmod +x SqlStoredProceduresCrudGenerator`.
3. Run `./SqlStoredProceduresCrudGenerator`.

The application requires a graphical desktop session and the native libraries
required by Avalonia on your distribution.

## macOS

Choose `osx-arm64` for Apple Silicon or `osx-x64` for an Intel Mac. Extract the
archive and move **SQL Stored Procedures CRUD Generator.app** to Applications.

Preview builds are not yet code-signed or notarized. macOS may require you to
Control-click the app, choose **Open**, and confirm the first launch. Never
disable system-wide Gatekeeper protections.

## Database permissions

Database discovery requires access to `master` and `HAS_DBACCESS` visibility.
Generated-procedure browsing requires metadata visibility in the selected database.
Installing `sp_CRUDGen` and creating procedures additionally require the
corresponding DDL permissions. Use a non-production database first and grant
only the minimum required privileges.
