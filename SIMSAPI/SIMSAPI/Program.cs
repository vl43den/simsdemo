
namespace SIMSAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthorization();


            app.MapControllers();

            // --- Temporary Database Initialization ---
            _ = Task.Run(() => 
            {
                try
                {
                    string input = Environment.GetEnvironmentVariable("postgresdb") ?? Environment.GetEnvironmentVariable("postgres") ?? "";
                    string connString = input;
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
                            connString = $"Host={host};Port={port};Username={user};Password={pass};Database={db};SslMode=Require;";
                        }
                        catch { }
                    }

                    if (!string.IsNullOrEmpty(connString))
                    {
                        using var conn = new Npgsql.NpgsqlConnection(connString);
                        conn.Open();
                        using var cmd = new Npgsql.NpgsqlCommand(@"
create schema IF NOT EXISTS sims;
SET search_path TO sims;

DO
$do$
BEGIN
   IF NOT EXISTS (
      SELECT FROM pg_catalog.pg_roles
      WHERE  rolname = 'appuser') THEN
      CREATE ROLE appuser LOGIN;
   END IF;
END
$do$;

CREATE TABLE IF NOT EXISTS incidentType (
    incident_type_id SERIAL PRIMARY KEY,
    name VARCHAR(50) NOT NULL,
    description TEXT
);

CREATE TABLE IF NOT EXISTS incident (
    incident_id SERIAL PRIMARY KEY,
    resolved BOOLEAN DEFAULT FALSE,
    reporter VARCHAR(50),
    reported_at TIMESTAMP,
    description TEXT,
    title VARCHAR(100),
    incident_type_id INT REFERENCES incidentType(incident_type_id),
    resource_id VARCHAR(100)
);

CREATE TABLE IF NOT EXISTS simsuser (
    user_id SERIAL PRIMARY KEY,
    IsActive BOOLEAN DEFAULT FALSE,
    IsAdmin BOOLEAN DEFAULT FALSE,
    LastLogin TIMESTAMP,
    Username VARCHAR(50),
    PWDHash VARCHAR(200)
);

GRANT USAGE ON SCHEMA sims TO appuser;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA sims TO appuser;
GRANT pg_read_all_data TO appuser;

INSERT INTO sims.incidenttype (name)
SELECT 'ticket' WHERE NOT EXISTS (SELECT 1 FROM sims.incidenttype WHERE name = 'ticket');
INSERT INTO sims.incidenttype (name)
SELECT 'issue' WHERE NOT EXISTS (SELECT 1 FROM sims.incidenttype WHERE name = 'issue');
INSERT INTO sims.incidenttype (name)
SELECT 'informational' WHERE NOT EXISTS (SELECT 1 FROM sims.incidenttype WHERE name = 'informational');
INSERT INTO sims.incidenttype (name)
SELECT 'problem' WHERE NOT EXISTS (SELECT 1 FROM sims.incidenttype WHERE name = 'problem');

INSERT INTO sims.simsuser(IsAdmin, IsActive, Username, PWDHash, LastLogin)
SELECT true, true, 'admin', '0afd8f3eb241eaf2d2b4191f0bdf29ac88ab3c612a1cc78a0cabbdb6e32f54ba', CURRENT_TIMESTAMP
WHERE NOT EXISTS (SELECT 1 FROM sims.simsuser WHERE Username = 'admin');

INSERT INTO sims.simsuser(IsAdmin, IsActive, Username, PWDHash, LastLogin)
SELECT false, true, 'user', '04f8996da763b7a969b1028ee3007569eaf3a635486ddab211d512c85b9df8fb', CURRENT_TIMESTAMP
WHERE NOT EXISTS (SELECT 1 FROM sims.simsuser WHERE Username = 'user');
", conn);
                        cmd.ExecuteNonQuery();
                        Console.WriteLine("Database initialized successfully.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Database initialization failed: " + ex.Message);
                }
            });
            // ------------------------------------------

            app.MapGet("/", () => "API is healthy");

            app.Run();
        }
    }
}