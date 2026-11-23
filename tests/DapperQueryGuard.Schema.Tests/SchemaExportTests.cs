using DapperQueryGuard.Core;

namespace DapperQueryGuard.Schema.Tests;

public sealed class SchemaExportTests
{
    [Fact]
    public void Export_format_is_deterministic_for_sorted_input()
    {
        var snapshot = new SchemaSnapshot(1, new[]
        {
            new SchemaTable("dbo", "A", Array.Empty<SchemaColumn>(), Array.Empty<SchemaIndex>()),
            new SchemaTable("dbo", "B", Array.Empty<SchemaColumn>(), Array.Empty<SchemaIndex>())
        });
        Assert.Equal(snapshot.ToJson(), SchemaSnapshot.FromJson(snapshot.ToJson()).ToJson());
    }
}
