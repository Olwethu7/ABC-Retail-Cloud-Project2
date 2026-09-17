using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace AzureStorageProject.Models
{
    public class TransactionViewModel
    {
        [Required(ErrorMessage = "Order ID is required")]
        public string OrderId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Customer name is required")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Product ID is required")]
        public string ProductId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; } = 1;

        public IFormFile? Receipt { get; set; }
    }

    public class TransactionResultViewModel
    {
        public string StepName { get; set; } = string.Empty;
        public string AzureService { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
