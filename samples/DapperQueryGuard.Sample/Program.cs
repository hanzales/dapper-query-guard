using Dapper;
using Microsoft.Data.SqlClient;
await using var connection = new SqlConnection("Server=(localdb)\\MSSQLLocalDB;Database=Demo;Trusted_Connection=True;TrustServerCertificate=True");
var customerCode = args.FirstOrDefault() ?? "C-001";
// With a schema snapshot containing dbo.Customers.CustomerCode VARCHAR(50), DQG001 is reported here.
var rows = await connection.QueryAsync<Customer>("SELECT * FROM dbo.Customers WHERE CustomerCode = @Code", new { Code = customerCode });
Console.WriteLine($"Rows: {rows.Count()}");
public sealed record Customer(int Id, string CustomerCode);
