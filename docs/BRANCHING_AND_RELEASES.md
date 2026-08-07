# Branching and releases

## Permanent branches

- `main` is the protected, releasable branch. Every commit must build and pass
  tests. Version tags and GitHub releases are created from `main` only.
- `develop` is the protected integration branch for the next release.

Keeping only two permanent branches reduces drift and maintenance overhead.

## Short-lived branches

Create these from `develop` and delete them after merging:

- `feature/<issue>-short-description` for new behavior.
- `fix/<issue>-short-description` for ordinary bug fixes.
- `docs/<issue>-short-description` for documentation-only changes.
- `chore/<issue>-short-description` for maintenance and infrastructure.

Create `release/vX.Y.Z` from `develop` only while stabilizing a release. Merge
it into `main`, tag the merge commit, then merge `main` back into `develop`.

Create `hotfix/vX.Y.Z` from `main` for urgent production fixes. Merge it into
both `main` and `develop`, then tag the `main` commit.

## Pull requests

1. Open ordinary pull requests against `develop`.
2. Keep changes focused and use a clear conventional title.
3. Require CI and CodeQL to pass before merge.
4. Prefer squash merging for short-lived branches.
5. Use a merge commit for `release/*` and `hotfix/*` branches when preserving
   the release boundary is valuable.

## Creating a release

1. Update `VersionPrefix` in the desktop project and move changelog entries out
   of `Unreleased`.
2. Merge the release pull request into `main`.
3. Create an annotated semantic-version tag, for example `v0.2.0`.
4. Push the tag. GitHub Actions builds self-contained Windows, Linux, Intel Mac,
   and Apple Silicon Mac archives, calculates SHA-256 checksums, and publishes a
   GitHub release.

```powershell
git tag -a v0.2.0 -m "SQL Stored Procedures CRUD Generator v0.2.0"
git push origin v0.2.0
```

Never move or reuse a published version tag.
