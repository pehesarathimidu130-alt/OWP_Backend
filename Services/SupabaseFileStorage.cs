using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Backend.Services
{
    public class SupabaseFileStorage : IFileStorage
    {
        private readonly HttpClient _httpClient;
        private readonly string _url;
        private readonly string _bucket;
        private readonly string _secretKey;
        private readonly IWebHostEnvironment _environment;

        public SupabaseFileStorage(HttpClient httpClient, IConfiguration config, IWebHostEnvironment environment)
        {
            _httpClient = httpClient;
            _environment = environment;
            
            var rawUrl = config["Supabase:Url"] ?? "";
            // Normalize URL (strip trailing slash and any /rest/v1 segment)
            var baseUrl = rawUrl.TrimEnd('/');
            if (baseUrl.EndsWith("/rest/v1", StringComparison.OrdinalIgnoreCase))
            {
                baseUrl = baseUrl.Substring(0, baseUrl.Length - 8);
            }
            _url = baseUrl;
            _bucket = config["Supabase:Bucket"] ?? "";
            _secretKey = config["Supabase:SecretKey"] ?? "";
        }

        public async Task<string> SaveAsync(IFormFile file, string folder, string fileName, string contentType)
        {
            using var stream = file.OpenReadStream();
            return await SaveAsync(stream, folder, fileName, contentType);
        }

        public async Task<string> SaveAsync(Stream stream, string folder, string fileName, string contentType)
        {
            var cleanFolder = folder.Trim('/');
            var objectPath = $"{cleanFolder}/{fileName}";
            var uploadUrl = $"{_url}/storage/v1/object/{_bucket}/{objectPath}";

            using var request = new HttpRequestMessage(HttpMethod.Post, uploadUrl);
            request.Headers.Add("apikey", _secretKey);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _secretKey);
            
            var content = new StreamContent(stream);
            content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorDetails = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to upload to Supabase: {response.StatusCode} - {errorDetails}");
            }

            return $"{_url}/storage/v1/object/public/{_bucket}/{objectPath}";
        }

        public async Task DeleteAsync(string? storedValue)
        {
            if (string.IsNullOrWhiteSpace(storedValue)) return;

            // Check if it's a Supabase URL
            var publicPrefix = $"{_url}/storage/v1/object/public/{_bucket}/";
            if (storedValue.StartsWith(publicPrefix, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var objectPath = storedValue.Substring(publicPrefix.Length);
                    var deleteUrl = $"{_url}/storage/v1/object/{_bucket}/{objectPath}";

                    using var request = new HttpRequestMessage(HttpMethod.Delete, deleteUrl);
                    request.Headers.Add("apikey", _secretKey);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _secretKey);

                    var response = await _httpClient.SendAsync(request);
                    // Don't throw if not found (already deleted), but log/ignore other errors gracefully
                }
                catch
                {
                    // Ignore errors (consistent with previous behavior)
                }
            }
            else
            {
                // Fallback: Delete local file if it's a relative /uploads/ path
                try
                {
                    if (storedValue.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return;

                    var rootPath = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    var cleanPath = storedValue.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                    var fullPath = Path.Combine(rootPath, cleanPath);

                    if (File.Exists(fullPath))
                    {
                        File.Delete(fullPath);
                    }
                }
                catch
                {
                    // Ignore errors
                }
            }
        }
    }
}
