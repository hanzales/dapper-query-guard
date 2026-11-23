using DapperQueryGuard.Schema;
if (args.Length >= 2 && args[0].Equals("schema", StringComparison.OrdinalIgnoreCase) && args[1].Equals("export", StringComparison.OrdinalIgnoreCase))
{
    var envIndex = Array.IndexOf(args, "--connection-env"); var outputIndex = Array.IndexOf(args, "--output");
    if (envIndex < 0 || outputIndex < 0 || envIndex + 1 >= args.Length || outputIndex + 1 >= args.Length) return Usage();
    var value = Environment.GetEnvironmentVariable(args[envIndex + 1]); if (string.IsNullOrWhiteSpace(value)) { Console.Error.WriteLine("The named connection environment variable is missing."); return 2; }
    try { var snapshot = await SqlServerSchemaExporter.ExportAsync(value); await File.WriteAllTextAsync(args[outputIndex + 1], snapshot.ToJson()); Console.WriteLine($"Exported {snapshot.Tables.Count} tables to {args[outputIndex + 1]}"); return 0; } catch (Exception ex) { Console.Error.WriteLine($"Schema export failed: {ex.Message}"); return 2; }
}
return Usage();
static int Usage() { Console.WriteLine("DapperQueryGuard CLI\n\n  dqg schema export --connection-env DQG_CONNECTION --output dapperguard.schema.json"); return 2; }
