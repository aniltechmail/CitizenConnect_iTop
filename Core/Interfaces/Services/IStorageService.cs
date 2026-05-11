using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces.Services
{
    public interface IStorageService
    {
        Task<string> SaveFileAsync(Stream fileStream,string fileName,string folder);
        Task DeleteFileAsync(string filePath);
        string GetFileUrl(string filePath);
    }
}
