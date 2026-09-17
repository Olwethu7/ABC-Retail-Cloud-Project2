using Microsoft.AspNetCore.Mvc;
using AzureStorageProject.Models;
using AzureStorageProject.Services;

namespace AzureStorageProject.Controllers
{
    public class ProductController : Controller
    {
        private readonly IStorageService _storageService;
        private const string TABLE_NAME = "Products";
        private const string CONTAINER_NAME = "productimages";

        public ProductController(IStorageService storageService)
        {
            _storageService = storageService;
        }

        // GET: Product
        public async Task<IActionResult> Index()
        {
            var products = await _storageService.GetAllEntitiesAsync<ProductInfo>(TABLE_NAME);
            return View(products);
        }

        // GET: Product/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductInfo product, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                if (string.IsNullOrEmpty(product.RowKey))
                {
                    product.RowKey = $"PROD-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
                }
                product.PartitionKey = "Product";

                // Handle image upload
                if (imageFile != null && imageFile.Length > 0)
                {
                    string fileName = $"{Guid.NewGuid()}_{imageFile.FileName}";
                    using (var stream = imageFile.OpenReadStream())
                    {
                        var blobUrl = await _storageService.UploadBlobAsync(CONTAINER_NAME, fileName, stream);
                        product.ImageUrl = blobUrl;
                    }
                }

                await _storageService.AddEntityAsync(TABLE_NAME, product);
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        // GET: Product/Edit/5
        public async Task<IActionResult> Edit(string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
            {
                return NotFound();
            }

            var product = await _storageService.GetEntityAsync<ProductInfo>(TABLE_NAME, partitionKey, rowKey);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // POST: Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProductInfo product, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                product.PartitionKey = "Product";

                // Handle image upload
                if (imageFile != null && imageFile.Length > 0)
                {
                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(product.ImageUrl))
                    {
                        string oldFileName = product.ImageUrl.Split('/').Last();
                        await _storageService.DeleteBlobAsync(CONTAINER_NAME, oldFileName);
                    }

                    // Upload new image
                    string fileName = $"{Guid.NewGuid()}_{imageFile.FileName}";
                    using (var stream = imageFile.OpenReadStream())
                    {
                        var blobUrl = await _storageService.UploadBlobAsync(CONTAINER_NAME, fileName, stream);
                        product.ImageUrl = blobUrl;
                    }
                }

                await _storageService.UpdateEntityAsync(TABLE_NAME, product);
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        // GET: Product/Delete/5
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
            {
                return NotFound();
            }

            var product = await _storageService.GetEntityAsync<ProductInfo>(TABLE_NAME, partitionKey, rowKey);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // POST: Product/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            // Get product to delete its image
            var product = await _storageService.GetEntityAsync<ProductInfo>(TABLE_NAME, partitionKey, rowKey);
            if (product != null && !string.IsNullOrEmpty(product.ImageUrl))
            {
                string fileName = product.ImageUrl.Split('/').Last();
                await _storageService.DeleteBlobAsync(CONTAINER_NAME, fileName);
            }

            await _storageService.DeleteEntityAsync(TABLE_NAME, partitionKey, rowKey);
            return RedirectToAction(nameof(Index));
        }
    }
}