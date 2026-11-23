using System.Text.Json;
using System.Text.Json.Serialization;

namespace DapperQueryGuard.Core;

/// <summary>Versioned, offline SQL Server schema snapshot.</summary>
public sealed record SchemaSnapshot(int SchemaVersion, IReadOnlyList<SchemaTable> Tables)
{
    /// <summary>Current snapshot schema version.</summary>
    public const int CurrentVersion = 1;
    /// <summary>Finds an exact table by schema and name.</summary>
    public SchemaTable? FindTable(string schema, string name) => Tables.FirstOrDefault(t => string.Equals(t.Schema, schema, StringComparison.OrdinalIgnoreCase) && string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
    /// <summary>Serializes stable indented JSON.</summary>
    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);
    /// <summary>Reads and validates a snapshot.</summary>
    public static SchemaSnapshot FromJson(string json)
    {
        var snapshot = JsonSerializer.Deserialize<SchemaSnapshot>(json, JsonOptions) ?? throw new InvalidDataException("Schema snapshot is empty.");
        if (snapshot.SchemaVersion != CurrentVersion) throw new InvalidDataException($"Unsupported schema version: {snapshot.SchemaVersion}.");
        return snapshot;
    }
    private static JsonSerializerOptions JsonOptions => new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, Converters = { new JsonStringEnumConverter() } };
}
/// <summary>SQL Server table metadata.</summary>
public sealed record SchemaTable(string Schema, string Name, IReadOnlyList<SchemaColumn> Columns, IReadOnlyList<SchemaIndex> Indexes);
/// <summary>SQL Server column metadata.</summary>
public sealed record SchemaColumn(string Name, SqlTypeKind Type, int? Length, bool IsNullable, string? Collation);
/// <summary>Relevant index metadata.</summary>
public sealed record SchemaIndex(string Name, bool IsUnique, IReadOnlyList<string> Columns);
/// <summary>Normalized SQL Server types.</summary>
public enum SqlTypeKind { Unknown, Varchar, Nvarchar, Char, Nchar, Text, Ntext, Int, BigInt, SmallInt, TinyInt, Decimal, Numeric, Bit, Date, DateTime, DateTime2, UniqueIdentifier, Binary, Varbinary, Other }
