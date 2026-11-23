# Getting started

Install the analyzer and code-fix packages, export a snapshot with `dotnet run --project src/DapperQueryGuard.Cli -- schema export`, and include the resulting JSON as an `AdditionalFiles` item. Review DQG001 warnings against the real schema and query plan before applying a code fix.
