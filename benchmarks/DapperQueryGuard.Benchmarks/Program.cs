using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace DapperQueryGuard.Benchmarks;

public static class Program
{
    public static void Main() => BenchmarkRunner.Run<SnapshotSerializationBenchmark>();
}

[MemoryDiagnoser]
public class SnapshotSerializationBenchmark
{
    private readonly string _json = "{\"schemaVersion\":1,\"tables\":[]}";

    [Benchmark]
    public object ParseSnapshot() => System.Text.Json.JsonDocument.Parse(_json);
}
