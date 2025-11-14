using Azure.Storage.Blobs.Models;

namespace ABCRetailers.Functions.Services
{
    public interface IBlobStorageService
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);
        Task<Stream> DownloadFileAsync(string fileName);
        Task<bool> DeleteFileAsync(string fileName);
        Task<List<BlobItem>> ListFilesAsync(string? prefix = null);
        Task<string> GetFileUrlAsync(string fileName);
    }
}