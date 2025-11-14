using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using ABCRetailers.Functions.Services;
using System.Net.Http;
using System.Linq;

namespace ABCRetailers.Functions
{
    public class FileManagementFunction
    {
        private readonly ILogger<FileManagementFunction> _logger;
        private readonly IBlobStorageService _blobService;
        private readonly IFileShareService _fileShareService;

        public FileManagementFunction(ILogger<FileManagementFunction> logger, IBlobStorageService blobService, IFileShareService fileShareService)
        {
            _logger = logger;
            _blobService = blobService;
            _fileShareService = fileShareService;
        }

        [Function("UploadProductImage")]
        public async Task<HttpResponseData> UploadProductImage(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "files/upload/image")] HttpRequestData req)
        {
            try
            {
                var fileName = $"image_{Guid.NewGuid()}.jpg";
                var contentType = "image/jpeg";

                using var fileStream = new MemoryStream();
                var fileUrl = await _blobService.UploadFileAsync(fileStream, fileName, contentType);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteStringAsync(JsonSerializer.Serialize(new
                {
                    success = true,
                    fileName = fileName,
                    fileUrl = fileUrl,
                    message = "Image uploaded successfully"
                }));
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading product image");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("Internal server error");
                return errorResponse;
            }
        }

        // (keep your other [Function(...)] methods here)
    }
}
