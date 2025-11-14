using Azure.Storage.Files.Shares.Models;

namespace ABCRetailers.Functions.Services
{
    public interface IFileShareService
    {
        Task<string> UploadContractAsync(Stream fileStream, string fileName);
        Task<Stream> DownloadContractAsync(string fileName);
        Task<bool> DeleteContractAsync(string fileName);
        Task<List<ShareFileItem>> ListContractsAsync();
        Task<string> GetContractUrlAsync(string fileName);
    }
}