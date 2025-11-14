using Microsoft.AspNetCore.Mvc;
using ABCRetailers.Attributes;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Http;

namespace ABCRetailers.Controllers
{
    [RequireLogin(Roles = "Admin")]
    public class FileManagementController : Controller
    {
        private readonly IAzureFunctionsService _functionsService;

        public FileManagementController(IAzureFunctionsService functionsService)
        {
            _functionsService = functionsService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var images = await _functionsService.GetProductImagesAsync();
                var contracts = await _functionsService.GetContractsAsync();

                ViewBag.Images = images;
                ViewBag.Contracts = contracts;

                return View();
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading files: {ex.Message}";
                return View();
            }
        }

        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select a file to upload.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                using var stream = file.OpenReadStream();
                var imageUrl = await _functionsService.UploadProductImageAsync(
                    stream,
                    file.FileName,
                    file.ContentType);

                if (!string.IsNullOrEmpty(imageUrl))
                {
                    TempData["Success"] = "Image uploaded successfully!";
                }
                else
                {
                    TempData["Error"] = "Failed to upload image.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error uploading image: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> UploadContract(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select a file to upload.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                using var stream = file.OpenReadStream();
                var fileUrl = await _functionsService.UploadContractAsync(stream, file.FileName);

                if (!string.IsNullOrEmpty(fileUrl))
                {
                    TempData["Success"] = "Contract uploaded successfully!";
                }
                else
                {
                    TempData["Error"] = "Failed to upload contract.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error uploading contract: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteFile(string fileType, string fileName)
        {
            try
            {
                var success = await _functionsService.DeleteFileAsync(fileType, fileName);

                if (success)
                {
                    TempData["Success"] = "File deleted successfully!";
                }
                else
                {
                    TempData["Error"] = "Failed to delete file.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting file: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> DownloadImage(string fileName)
        {
            try
            {
                // In a real implementation, you would stream the file content
                // For now, we'll redirect to the blob URL
                var images = await _functionsService.GetProductImagesAsync();
                var image = images.FirstOrDefault(i => i.Name == fileName);

                if (image != null)
                {
                    return Redirect(image.Url);
                }
                else
                {
                    TempData["Error"] = "Image not found.";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error downloading image: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> DownloadContract(string fileName)
        {
            try
            {
                // In a real implementation, you would stream the file content
                // For now, we'll redirect to the file share URL
                var contracts = await _functionsService.GetContractsAsync();
                var contract = contracts.FirstOrDefault(c => c.Name == fileName);

                if (contract != null)
                {
                    return Redirect(contract.Url);
                }
                else
                {
                    TempData["Error"] = "Contract not found.";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error downloading contract: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
