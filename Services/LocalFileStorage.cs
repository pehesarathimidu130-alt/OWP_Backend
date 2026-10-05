using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;

namespace Backend.Services
{
    public class LocalFileStorage : IFileStorage
    {
        private readonly IWebHostEnvironment _environment;

        public LocalFileStorage(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<string> SaveAsync(IFormFile file, string folder, string fileName, string contentType)
        {
            using var stream = file.OpenReadStream();
            return await SaveAsync(stream, folder, fileName, contentType);
        }

        public async Task<string> SaveAsync(Stream stream, string folder, string fileName, string contentType)
        {
            var rootPath = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var directory = Path.Combine(rootPath, "uploads", folder.Replace("/", Path.DirectorySeparatorChar.ToString()));
            
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var destinationPath = Path.Combine(directory, fileName);
            using (var fileStream = File.Create(destinationPath))
            {
                await stream.CopyToAsync(fileStream);
            }

            // Return relative URL
            var cleanFolder = folder.Trim('/');
            return $"/uploads/{cleanFolder}/{fileName}";
        }

        public Task DeleteAsync(string? storedValue)
        {
            if (string.IsNullOrWhiteSpace(storedValue)) return Task.CompletedTask;

            try
            {
                // Only handle relative /uploads/ paths
                if (storedValue.StartsWith("http", System.StringComparison.OrdinalIgnoreCase))
                {
                    return Task.CompletedTask;
                }

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
                // Ignore delete errors (consistent with previous behavior)
            }

            return Task.CompletedTask;
        }
    }
}
