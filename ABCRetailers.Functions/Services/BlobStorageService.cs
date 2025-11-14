using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ABCRetailers.Functions.Services
{
    public class BlobStorageService : IBlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName = "product-images";

        public BlobStorageService(IConfiguration configuration)
        {
            var connectionString = configuration["AzureStorageConnectionString"] ?? "UseDevelopmentStorage=true";
            _blobServiceClient = new BlobServiceClient(connectionString);
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            var blobClient = containerClient.GetBlobClient(fileName);
            var blobHttpHeaders = new BlobHttpHeaders
            {
                ContentType = contentType
            };

            await blobClient.UploadAsync(fileStream, new BlobUploadOptions
            {
                HttpHeaders = blobHttpHeaders
            });

            return blobClient.Uri.ToString();
        }

        public async Task<Stream> DownloadFileAsync(string fileName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(fileName);
            var response = await blobClient.DownloadStreamingAsync();
            return response.Value.Content;
        }

        public async Task<bool> DeleteFileAsync(string fileName)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                var blobClient = containerClient.GetBlobClient(fileName);
                var response = await blobClient.DeleteIfExistsAsync();
                return response.Value;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<BlobItem>> ListFilesAsync(string? prefix = null)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobs = new List<BlobItem>();

            await foreach (var blob in containerClient.GetBlobsAsync(prefix: prefix))
            {
                blobs.Add(blob);
            }

            return blobs;
        }

        public Task<string> GetFileUrlAsync(string fileName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(fileName);
            return Task.FromResult(blobClient.Uri.ToString());
        }
    }
}