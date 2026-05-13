using Core.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Storage
{
    public class LocalStorageService : IStorageService
    {
        private readonly string _basePath;
        private readonly string _baseUrl;

        public LocalStorageService(IConfiguration config)
        {
            _basePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                config["FileStorage:BasePath"] ?? "uploads"
            );
            _baseUrl = config["FileStorage:BaseUrl"] ?? "http://localhost:5000/uploads";
        }

        public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string folder) 
        {
            var folderPath = Path.Combine(_basePath, folder);
            Directory.CreateDirectory(folderPath);

            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            var filePath = Path.Combine(folderPath, uniqueFileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await fileStream.CopyToAsync(stream);

            // Return relative path for storage
            return Path.Combine(folder, uniqueFileName).Replace("\\", "/");
        }

        public Task DeleteFileAsync(string filePath)
        {
            var fullPath = Path.Combine(_basePath, filePath);
            if (File.Exists(fullPath))
                File.Delete(fullPath);
            return Task.CompletedTask;
        }

        public string GetFileUrl(string filePath) =>
            $"{_baseUrl}/{filePath}";
    }
}
