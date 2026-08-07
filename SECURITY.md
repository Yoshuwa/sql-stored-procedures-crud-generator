# Security policy

## Supported versions

Security fixes are applied to the latest published release and the `main`
branch. Older preview releases may not receive patches.

## Reporting a vulnerability

Please do not open a public issue for a suspected vulnerability. Report it
privately through a
[GitHub security advisory](https://github.com/Yoshuwa/sql-stored-procedures-crud-generator/security/advisories/new).

Include a clear description, affected version, reproduction steps, and impact.
Remove all real credentials, connection strings, and private database metadata.
Maintainers will acknowledge a report as soon as practical, investigate it,
coordinate a fix, and credit the reporter unless anonymity is requested.

## Security boundaries

The application connects to databases using permissions supplied by the user.
Run it with the least SQL Server privilege needed, review generated SQL, and
test changes outside production. Credentials are not intentionally persisted.
