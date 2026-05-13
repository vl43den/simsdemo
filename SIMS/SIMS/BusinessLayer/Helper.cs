using RestSharp;
using System.Security.Cryptography;
using System.Text;

namespace SIMS
{
    public static class Helper
    {
        static string APIURL = Environment.GetEnvironmentVariable("api") ?? Environment.GetEnvironmentVariable("API") ?? Environment.GetEnvironmentVariable("API_URL") ?? "http://localhost:8888";
        
        public static string URL_getToken = $"{APIURL}/AuthService";
        public static string URL_checkToken = $"{APIURL}/AuthService/check";

        public static async Task<string> getToken(string username, string password)
        {
            RestClient client = new RestClient(Helper.URL_getToken);
            RestRequest request = new RestRequest("", Method.Post);
            request.AddJsonBody(new { username = username, password = password });
            RestResponse response = await client.ExecuteAsync(request);
            string token = response.Content ?? "";
            token = token.Replace("\"", "");
            return token;
        }

        public static async Task<bool> checkToken(string username, string token)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(token)) return false;
            RestClient client = new RestClient(Helper.URL_checkToken);
            RestRequest request = new RestRequest("", Method.Get);
            request.AddQueryParameter("username", username);
            request.AddQueryParameter("token", token);
            RestResponse response = await client.ExecuteAsync(request);
            return response.IsSuccessful;
        }

        public static string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++) { builder.Append(bytes[i].ToString("x2")); }
                return builder.ToString();
            }
        }

        public static async Task<bool> EscalateIncident(string resourceId)
        {
            try 
            {
                if (string.IsNullOrEmpty(resourceId)) return false;
                string escalateApiUrl = Environment.GetEnvironmentVariable("ESCALATE_API_URL");
                string escalateApiKey = Environment.GetEnvironmentVariable("ESCALATE_API_KEY") ?? "";
                
                if (string.IsNullOrEmpty(escalateApiUrl)) return false;
                
                RestClient client = new RestClient(escalateApiUrl);
                RestRequest request = new RestRequest("", Method.Post);
                if (!string.IsNullOrEmpty(escalateApiKey))
                    request.AddHeader("x-api-key", escalateApiKey);
                
                request.AddJsonBody(new { resourceId = resourceId });
                RestResponse response = await client.ExecuteAsync(request);
                
                return response.IsSuccessful;
            }
            catch
            {
                return false;
            }
        }
    }

}
