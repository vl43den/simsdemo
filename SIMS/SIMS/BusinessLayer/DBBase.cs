namespace SIMS
{
    public class DBBase
    {
        public string ConnectionString { get; set; } = GetValidConnectionString();

        public static string GetValidConnectionString()
        {
            string input = Environment.GetEnvironmentVariable("postgresdb") ?? Environment.GetEnvironmentVariable("postgres") ?? throw new Exception("Environment variable 'postgresdb' is not set.");
            if (input.TrimStart().StartsWith("{"))
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(input);
                    var root = doc.RootElement;
                    string host = root.TryGetProperty("host", out var h) ? (h.GetString() ?? "sims-postgres.cluster-c44rcgsrem0h.eu-central-1.rds.amazonaws.com") : "sims-postgres.cluster-c44rcgsrem0h.eu-central-1.rds.amazonaws.com";
                    if (string.IsNullOrEmpty(host)) host = "sims-postgres.cluster-c44rcgsrem0h.eu-central-1.rds.amazonaws.com";
                    string user = root.TryGetProperty("username", out var u) ? u.GetString() : "";
                    string pass = root.TryGetProperty("password", out var p) ? p.GetString() : "";
                    string db = root.TryGetProperty("dbname", out var d) ? (d.GetString() ?? "postgres") : "postgres";
                    if (string.IsNullOrEmpty(db)) db = "postgres";
                    int port = root.TryGetProperty("port", out var pt) ? pt.GetInt32() : 5432;
                    return $"Host={host};Port={port};Username={user};Password={pass};Database={db};SslMode=Require;";
                }
                catch { return input; }
            }
            return input;
        }
    }
}
