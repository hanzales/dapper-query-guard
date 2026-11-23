# Architecture

`DapperSqlGuard.Core` contains dependency-light schema records and stable JSON serialization. `DapperSqlGuard.Schema` performs an explicit, read-only SQL Server catalog export. `DapperSqlGuard.Analyzers` consumes that JSON through Roslyn `AdditionalFiles`; compilation never opens a database connection. `DapperSqlGuard.CodeFixes` supplies a separate IDE package so the analyzer remains usable in build-only environments. The CLI is a small .NET 10 executable around the exporter.

DQG001 uses a deliberately narrow parser for static single-table equality predicates. A future parser can replace it without changing snapshot or diagnostic contracts.
