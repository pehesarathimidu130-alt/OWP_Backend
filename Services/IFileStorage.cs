using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;

namespace Backend.Services
{
    public interface IFileStorage
    {
        Task<string> SaveAsync(IFormFile file, string folder, string fileName, string contentType);
        Task<string> SaveAsync(Stream stream, string folder, string fileName, string contentType);
        Task DeleteAsync(string? storedValue);
    }
}
