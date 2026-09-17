using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace AzureStorageProject.Models
{
    public class UploadViewModel
    {
        [Required(ErrorMessage = "Please select an image file")]
        public IFormFile? File { get; set; }

        public string? ProductId { get; set; }
    }
}