using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using Npgsql;

namespace SIMSAPI.Controllers
{
    public class LoginRequest
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }

    [ApiController]
    [Route("[controller]")]
    public class AuthService : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok();
        }

        [HttpGet]
        [Route("check")]
        public IActionResult Get(string username, string token)
        {
            if (new RedisDB().CheckToken(username, token) == true)
            {
                return Ok();
            }

            else
            {
                return BadRequest();
            }
        }

        [HttpPost]
        public string Post([FromBody] LoginRequest request)
        {
            if (checkUser(request.Username, request.Password))
            {
                string token = generateToken(request.Username, request.Password);
                new RedisDB().StoreToken(request.Username, token);
                return token;
            }
            else
            {
                return "";
            }
        }

        private string generateToken(string username, string password)
        {
            //HACK generate cool Token ;-) -> base64 is not encryption!
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(username + password + DateTime.Now.ToString()));
        }

        private bool checkUser(string username, string password)
        {
            try
            {
                bool result = false;
                string input = Environment.GetEnvironmentVariable("postgresdb") ?? Environment.GetEnvironmentVariable("postgres") ?? throw new Exception("Environment variable 'postgresdb' is not set.");
                string connectionString = input;
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
                        connectionString = $"Host={host};Port={port};Username={user};Password={pass};Database={db};SslMode=Require;";
                    }
                    catch { }
                }


                using (NpgsqlConnection db = new NpgsqlConnection(connectionString))
                {
                    db.Open();
                    using (NpgsqlCommand cmd = new NpgsqlCommand($"select * from sims.simsuser where username = @username and pwdhash = @pwdhash", db))
                    {
                        cmd.Parameters.AddWithValue("username", username);
                        cmd.Parameters.AddWithValue("pwdhash", computeSha256Hash(password));
                        using (NpgsqlDataReader reader = cmd.ExecuteReader())
                        {
                            result = reader.HasRows;
                        }
                    }
                    db.Close();
                }

                return result;
            }
            catch
            {
                return false;
            }
        }

        private string computeSha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++) { builder.Append(bytes[i].ToString("x2")); }
                return builder.ToString();
            }

        }
    }
}