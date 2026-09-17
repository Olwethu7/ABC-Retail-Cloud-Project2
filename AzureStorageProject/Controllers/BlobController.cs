using Microsoft.AspNetCore.Mvc;
using AzureStorageProject.Models;
using AzureStorageProject.Services;

namespace AzureStorageProject.Controllers
{
    public class BlobController : Controller
    {
        private readonly IStorageService _storageService;
        private const string CONTAINER_NAME = "productimages";

        public BlobController(IStorageService storageService)
        {
            _storageService = storageService;
        }

        // GET: Blob
        public async Task<IActionResult> Index()
        {
            var blobs = await _storageService.ListBlobsAsync(CONTAINER_NAME);
            return View(blobs);
        }

        // GET: Blob/ViewImage/5
        public async Task<IActionResult> ViewImage(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return NotFound();
            }

            try
            {
                var stream = await _storageService.DownloadBlobAsync(CONTAINER_NAME, fileName);
                if (stream == null)
                {
                    return NotFound();
                }

                string contentType = "image/jpeg";
                if (fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    contentType = "image/png";
                else if (fileName.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
                    contentType = "image/gif";
                else if (fileName.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                    contentType = "image/svg+xml";
                else if (fileName.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
                    contentType = "image/webp";

                return File(stream, contentType);
            }
            catch (Exception)
            {
                return NotFound();
            }
        }

        // GET: Blob/Upload
        public IActionResult Upload()
        {
            return View();
        }

        // POST: Blob/Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(UploadViewModel model)
        {
            if (model.File != null && model.File.Length > 0)
            {
                if (string.IsNullOrEmpty(model.ProductId))
                {
                    ViewBag.Message = "⚠️ Please enter a Product ID.";
                    return View(model);
                }

                string fileName = $"{Guid.NewGuid()}_{model.File.FileName}";

                using (var stream = model.File.OpenReadStream())
                {
                    var blobUrl = await _storageService.UploadBlobAsync(CONTAINER_NAME, fileName, stream);

                    try
                    {
                        var product = await _storageService.GetEntityAsync<ProductInfo>("Products", "Product", model.ProductId);
                        if (product != null)
                        {
                            product.ImageUrl = blobUrl;
                            await _storageService.UpdateEntityAsync("Products", product);
                            ViewBag.Message = $"✅ Image linked to product: {product.Name}";

                            // Clear the model after successful upload
                            ModelState.Clear();
                            return View(new UploadViewModel());
                        }
                        else
                        {
                            // Delete the uploaded image since product not found
                            await _storageService.DeleteBlobAsync(CONTAINER_NAME, fileName);
                            ViewBag.Message = $"❌ Product ID '{model.ProductId}' not found. Please check the Product Code.";
                            return View(model);
                        }
                    }
                    catch (Exception)
                    {
                        // Delete the uploaded image since product not found
                        await _storageService.DeleteBlobAsync(CONTAINER_NAME, fileName);
                        ViewBag.Message = $"❌ Product ID '{model.ProductId}' not found. Please check the Product Code.";
                        return View(model);
                    }
                }
            }
            else
            {
                ViewBag.Message = "⚠️ Please select a file to upload.";
            }

            return View(model);
        }

        // GET: Blob/Delete/5
        public async Task<IActionResult> Delete(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return NotFound();
            }

            var products = await _storageService.GetAllEntitiesAsync<ProductInfo>("Products");
            var productUsingImage = products.FirstOrDefault(p => p.ImageUrl?.Contains(fileName) == true);

            if (productUsingImage != null)
            {
                // Remove image from product first
                productUsingImage.ImageUrl = string.Empty;
                await _storageService.UpdateEntityAsync("Products", productUsingImage);
            }

            await _storageService.DeleteBlobAsync(CONTAINER_NAME, fileName);
            return RedirectToAction(nameof(Index));
        }
    }
}