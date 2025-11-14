using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ABCRetailers.Functions.Services
{
    public class FileShareService : IFileShareService
    {
        private readonly ShareServiceClient _shareServiceClient;
        private readonly string _shareName = "contracts";

        public FileShareService(IConfiguration configuration)
        {
            var connectionString = configuration["AzureStorageConnectionString"] ?? "UseDevelopmentStorage=true";
            _shareServiceClient = new ShareServiceClient(connectionString);
        }

        public async Task<string> UploadContractAsync(Stream fileStream, string fileName)
        {
            var shareClient = _shareServiceClient.GetShareClient(_shareName);
            await shareClient.CreateIfNotExistsAsync();

            var directoryClient = shareClient.GetRootDirectoryClient();
            var fileClient = directoryClient.GetFileClient(fileName);

            await fileClient.CreateAsync(fileStream.Length);
            await fileClient.UploadRangeAsync(new HttpRange(0, fileStream.Length), fileStream);

            return fileClient.Uri.ToString();
        }

        public async Task<Stream> DownloadContractAsync(string fileName)
        {
            var shareClient = _shareServiceClient.GetShareClient(_shareName);
            var directoryClient = shareClient.GetRootDirectoryClient();
            var fileClient = directoryClient.GetFileClient(fileName);
            var response = await fileClient.DownloadAsync();
            return response.Value.Content;
        }

        public async Task<bool> DeleteContractAsync(string fileName)
        {
            try
            {
                var shareClient = _shareServiceClient.GetShareClient(_shareName);
                var directoryClient = shareClient.GetRootDirectoryClient();
                var fileClient = directoryClient.GetFileClient(fileName);
                var response = await fileClient.DeleteIfExistsAsync();
                return response.Value;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<ShareFileItem>> ListContractsAsync()
        {
            var shareClient = _shareServiceClient.GetShareClient(_shareName);
            var directoryClient = shareClient.GetRootDirectoryClient();
            var files = new List<ShareFileItem>();

            await foreach (var file in directoryClient.GetFilesAndDirectoriesAsync())
            {
                if (file.IsDirectory == false)
                {
                    files.Add(file);
                }
            }

            return files;
        }

        public Task<string> GetContractUrlAsync(string fileName)
        {
            var shareClient = _shareServiceClient.GetShareClient(_shareName);
            var directoryClient = shareClient.GetRootDirectoryClient();
            var fileClient = directoryClient.GetFileClient(fileName);
            return Task.FromResult(fileClient.Uri.ToString());
        }
    }
}