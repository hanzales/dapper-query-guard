# DapperQueryGuard

Schema-aware performance diagnostics for Dapper and SQL Server.

DapperQueryGuard is a conservative Roslyn analyzer that finds one easy-to-miss class of Dapper issue: a plain .NET `string` bound to a non-Unicode (`varchar`/`char`) SQL Server column. Dapper normally describes strings as Unicode. Depending on collation, indexes, statistics and data distribution, SQL Server may add an implicit conversion. The diagnostic reports a potential risk; it never claims that a query is guaranteed to scan or slow down.

## Quick start

Build the repository with .NET 10:

```bash
dotnet restore
dotnet build -c Release
dotnet test -c Release
```

Install the analyzer and code-fix packages from a local or NuGet feed when published:

```bash
dotnet add package Hanzales.DapperQueryGuard.Analyzers --prerelease
dotnet add package Hanzales.DapperQueryGuard.CodeFixes --prerelease
```

Export an offline schema snapshot (the CLI reads the connection string only from the named environment variable):

```bash
export DQG_CONNECTION='Server=localhost;Database=Shop;Integrated Security=true;TrustServerCertificate=true'
dotnet run --project src/DapperQueryGuard.Cli -- schema export \
  --connection-env DQG_CONNECTION --output dapperguard.schema.json
```

Add the snapshot to the consuming project as an AdditionalFiles item:

```xml
<AdditionalFiles Include="dapperguard.schema.json" />
```

The compiler never connects to SQL Server. Snapshots contain schema metadata only; credentials and row data are not exported.

## Example

```csharp
connection.Query<Customer>(
    "SELECT * FROM dbo.Customers WHERE CustomerCode = @Code",
    new { Code = customerCode });
```

For a verified `varchar(50) NOT NULL` column, DQG001 suggests a manual review. A safe fix may be:

```csharp
new { Code = new Dapper.DbString { Value = customerCode, IsAnsi = true, Length = 50 } }
```

The fix is intentionally conservative. It is not offered for nullable values, ambiguous SQL, `DynamicParameters`, stored procedures, custom type handlers, or cases where changing length/Unicode semantics could change behavior.

## Supported analysis

The initial release supports static, single-table `SELECT` statements with a simple equality predicate and anonymous-object parameters for common Dapper methods (`Query*`, `Execute*`). Unsupported SQL is ignored. Future rules are tracked in the roadmap.

## Packages

- `Hanzales.DapperQueryGuard.Core` — versioned schema models and serialization.
- `Hanzales.DapperQueryGuard.Schema` — read-only SQL Server catalog export.
- `Hanzales.DapperQueryGuard.Analyzers` — DQG001 Roslyn analyzer.
- `Hanzales.DapperQueryGuard.CodeFixes` — safe `Dapper.DbString` code fix.
- `DapperQueryGuard.Cli` — `dqg schema export` command (run with `dotnet run` in this preview).

## Security and privacy

Schema export uses read-only catalog queries and does not persist connection strings, credentials, table data, or parameter values. Treat snapshots as metadata that can still reveal object names. Review them before committing. The analyzer is a development aid, not a substitute for query-plan review, load testing, least privilege, or SQL Server monitoring.

## Limitations and roadmap

The parser is deliberately conservative and does not attempt full SQL semantic equivalence. Collation, computed expressions, views, stored procedures and provider-specific behavior can change the outcome. Planned diagnostics include parameter-size mismatches, non-SARGable expressions, oversized `IN` lists, numeric conversions and parameter-sensitive query risks.

See [docs/architecture.md](docs/architecture.md) and [ROADMAP.md](ROADMAP.md). Contributions are welcome under the MIT license.
