# Contributing

1. Open an issue for a proposed rule or behavior change.
2. Keep diagnostics conservative and include positive and negative tests.
3. Run `dotnet format --verify-no-changes` where available, then `dotnet test -c Release` and `dotnet pack -c Release`.
4. Do not commit connection strings, schema snapshots containing sensitive names, or benchmark claims without reproducible evidence.

Use conventional commit prefixes where practical (`feat:`, `fix:`, `docs:`, `test:`).
