using DapperQueryGuard.Core;
using Microsoft.Data.SqlClient;

namespace DapperQueryGuard.Schema;

/// <summary>Exports SQL Server schema metadata without reading application rows.</summary>
public sealed class SqlServerSchemaExporter
{
    /// <summary>Exports a read-only snapshot using the caller-supplied connection string.</summary>
    public static async Task<SchemaSnapshot> ExportAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var tables = new Dictionary<(string Schema, string Name), MutableTable>();
        const string columnsSql = "SELECT s.name,t.name,c.name,ty.name,c.max_length,c.is_nullable,c.collation_name FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id JOIN sys.columns c ON c.object_id=t.object_id JOIN sys.types ty ON ty.user_type_id=c.user_type_id ORDER BY s.name,t.name,c.column_id";
        await using (var command = new SqlCommand(columnsSql, connection)) await using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
        { while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) { var key = (reader.GetString(0), reader.GetString(1)); if (!tables.TryGetValue(key, out var table)) tables[key] = table = new MutableTable(key.Item1, key.Item2); table.Columns.Add(new SchemaColumn(reader.GetString(2), NormalizeType(reader.GetString(3)), NormalizeLength(reader.GetString(3), reader.GetInt16(4)), reader.GetBoolean(5), reader.IsDBNull(6) ? null : reader.GetString(6))); } }
        const string indexesSql = "SELECT s.name,t.name,i.name,i.is_unique,ic.key_ordinal,c.name FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id JOIN sys.indexes i ON i.object_id=t.object_id JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE i.is_hypothetical=0 AND i.name IS NOT NULL ORDER BY s.name,t.name,i.name,ic.key_ordinal";
        await using (var command = new SqlCommand(indexesSql, connection)) await using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
        { while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) { var key = (reader.GetString(0), reader.GetString(1)); if (!tables.TryGetValue(key, out var table)) continue; var index = table.Indexes.FirstOrDefault(x => x.Name == reader.GetString(2)); if (index is null) table.Indexes.Add(index = new MutableIndex(reader.GetString(2), reader.GetBoolean(3))); index.Columns.Add(reader.GetString(5)); } }
        return new SchemaSnapshot(SchemaSnapshot.CurrentVersion, tables.Values.OrderBy(x => x.Schema).ThenBy(x => x.Name).Select(x => x.ToImmutable()).ToArray());
    }
    private static SqlTypeKind NormalizeType(string type) => type.ToLowerInvariant() switch { "varchar" => SqlTypeKind.Varchar, "nvarchar" => SqlTypeKind.Nvarchar, "char" => SqlTypeKind.Char, "nchar" => SqlTypeKind.Nchar, "text" => SqlTypeKind.Text, "ntext" => SqlTypeKind.Ntext, "int" => SqlTypeKind.Int, "bigint" => SqlTypeKind.BigInt, "smallint" => SqlTypeKind.SmallInt, "tinyint" => SqlTypeKind.TinyInt, "decimal" => SqlTypeKind.Decimal, "numeric" => SqlTypeKind.Numeric, "bit" => SqlTypeKind.Bit, "date" => SqlTypeKind.Date, "datetime" => SqlTypeKind.DateTime, "datetime2" => SqlTypeKind.DateTime2, "uniqueidentifier" => SqlTypeKind.UniqueIdentifier, "binary" => SqlTypeKind.Binary, "varbinary" => SqlTypeKind.Varbinary, _ => SqlTypeKind.Other };
    private static int? NormalizeLength(string type, short bytes) => type.Equals("nvarchar", StringComparison.OrdinalIgnoreCase) || type.Equals("nchar", StringComparison.OrdinalIgnoreCase) ? bytes < 0 ? -1 : bytes / 2 : bytes < 0 ? -1 : bytes;
    private sealed class MutableTable(string schema, string name) { public string Schema { get; } = schema; public string Name { get; } = name; public List<SchemaColumn> Columns { get; } = []; public List<MutableIndex> Indexes { get; } = []; public SchemaTable ToImmutable() => new(Schema, Name, Columns.ToArray(), Indexes.Select(x => new SchemaIndex(x.Name, x.Unique, x.Columns.ToArray())).ToArray()); }
    private sealed class MutableIndex(string name, bool unique) { public string Name { get; } = name; public bool Unique { get; } = unique; public List<string> Columns { get; } = []; }
}
