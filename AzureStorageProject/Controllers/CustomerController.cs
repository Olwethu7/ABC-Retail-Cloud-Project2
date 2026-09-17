using Microsoft.AspNetCore.Mvc;
using AzureStorageProject.Models;
using AzureStorageProject.Services;

namespace AzureStorageProject.Controllers
{
    public class CustomerController : Controller
    {
        private readonly IStorageService _storageService;
        private const string TABLE_NAME = "Customers";

        public CustomerController(IStorageService storageService)
        {
            _storageService = storageService;
        }

        // GET: Customer
        public async Task<IActionResult> Index()
        {
            var customers = await _storageService.GetAllEntitiesAsync<CustomerProfile>(TABLE_NAME);
            return View(customers);
        }

        // GET: Customer/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Customer/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerProfile customer)
        {
            if (ModelState.IsValid)
            {
                if (string.IsNullOrEmpty(customer.RowKey))
                {
                    customer.RowKey = $"CUST-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
                }
                customer.PartitionKey = "Customer";

                await _storageService.AddEntityAsync(TABLE_NAME, customer);
                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }

        // GET: Customer/Edit/5
        public async Task<IActionResult> Edit(string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
            {
                return NotFound();
            }

            var customer = await _storageService.GetEntityAsync<CustomerProfile>(TABLE_NAME, partitionKey, rowKey);
            if (customer == null)
            {
                return NotFound();
            }
            return View(customer);
        }

        // POST: Customer/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CustomerProfile customer)
        {
            if (ModelState.IsValid)
            {
                await _storageService.UpdateEntityAsync(TABLE_NAME, customer);
                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }

        // GET: Customer/Delete/5
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
            {
                return NotFound();
            }

            var customer = await _storageService.GetEntityAsync<CustomerProfile>(TABLE_NAME, partitionKey, rowKey);
            if (customer == null)
            {
                return NotFound();
            }
            return View(customer);
        }

        // POST: Customer/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            await _storageService.DeleteEntityAsync(TABLE_NAME, partitionKey, rowKey);
            return RedirectToAction(nameof(Index));
        }
    }
}