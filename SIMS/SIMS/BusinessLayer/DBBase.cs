namespace SIMS
{
    public class DBBase
    {
        public string ConnectionString { get; set; } = Environment.GetEnvironmentVariable("postgresdb") ?? throw new Exception("Environment variable 'postgresdb' is not set.");
    }
}
