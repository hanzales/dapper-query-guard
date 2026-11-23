namespace DapperQueryGuard.PackageTests;

public sealed class PackageLayoutTests
{
    [Fact]
    public void Package_test_project_is_executable()
    {
        Assert.True(typeof(PackageLayoutTests).Assembly.GetName().Name!.Contains("PackageTests", StringComparison.Ordinal));
    }
}
