using DapperQueryGuard.Core;

namespace DapperQueryGuard.Core.Tests;

public sealed class SchemaSnapshotTests
{
    [Fact]
    public void Snapshot_round_trips_and_preserves_metadata()
    {
        var snapshot = new SchemaSnapshot(1, new[]
        {
            new SchemaTable("dbo", "Customers", new[]
            {
                new SchemaColumn("CustomerCode", SqlTypeKind.Varchar, 50, false, "SQL_Latin1_General_CP1_CI_AS")
            }, Array.Empty<SchemaIndex>())
        });

        var restored = SchemaSnapshot.FromJson(snapshot.ToJson());
        Assert.Equal(SqlTypeKind.Varchar, restored.FindTable("DBO", "customers")!.Columns[0].Type);
        Assert.Equal(50, restored.Tables[0].Columns[0].Length);
    }

    [Fact]
    public void Unsupported_schema_version_is_rejected()
    {
        Assert.Throws<InvalidDataException>(() => SchemaSnapshot.FromJson("{\"schemaVersion\":99,\"tables\":[]}"));
    }
}
