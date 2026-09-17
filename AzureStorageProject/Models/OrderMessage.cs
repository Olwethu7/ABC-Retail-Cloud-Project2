using System.ComponentModel.DataAnnotations;

namespace AzureStorageProject.Models
{
    public class OrderMessage
    {
        public string OrderId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Customer name is required")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Product ID is required")]
        public string ProductId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; } = 1;

        public string Action { get; set; } = string.Empty;
    }
}