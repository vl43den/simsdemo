using StackExchange.Redis;

namespace SIMSAPI
{
    public class RedisDB
    {
        private static ConnectionMultiplexer? _redis;
        private static readonly object _lock = new object();
        
        private string dbname = "";

        public RedisDB() 
        {
            string raw = Environment.GetEnvironmentVariable("redisdb") ?? Environment.GetEnvironmentVariable("redis") ?? throw new Exception("Environment variable 'redisdb' is not set.");
            
            // AWS Secrets Manager may return JSON like {"REDIS_URL":"rediss://host:port"}
            if (raw.TrimStart().StartsWith("{"))
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(raw);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("REDIS_URL", out var urlProp))
                        raw = urlProp.GetString() ?? raw;
                }
                catch { }
            }

            // Convert rediss://host:port or redis://host:port to StackExchange.Redis format
            if (raw.StartsWith("rediss://", StringComparison.OrdinalIgnoreCase))
            {
                string hostPort = raw.Substring("rediss://".Length).TrimEnd('/');
                dbname = $"{hostPort},ssl=true,sslprotocols=tls12|tls13,abortConnect=false,connectTimeout=10000";
            }
            else if (raw.StartsWith("redis://", StringComparison.OrdinalIgnoreCase))
            {
                string hostPort = raw.Substring("redis://".Length).TrimEnd('/');
                dbname = $"{hostPort},abortConnect=false";
            }
            else
            {
                dbname = raw;
            }

            if (_redis == null)
            {
                lock (_lock)
                {
                    if (_redis == null)
                    {
                        _redis = ConnectionMultiplexer.Connect(dbname);
                    }
                }
            }
        }

        public void StoreToken(string username, string token) 
        {
            try
            {
                IDatabase db = _redis!.GetDatabase();
                db.StringSet(username, token);
            }
            catch
            {
                throw;
            }
        }

        public bool CheckToken(string username, string token)
        {
            try
            {
                IDatabase db = _redis!.GetDatabase();
                return db.StringGet(username).ToString() == token ? true : false;
            }
            catch
            {
                throw;
            }
        }

    }
}
