using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using SemesterTwo.Models;
using SemesterTwo.Services;

namespace SemesterTwo.Controllers
{
    public class HomeController : Controller
    {
        private readonly BlobService _blobService;
        private readonly TableService _tableService;
        private readonly QueueService _queueService;
        private readonly FileService _fileService;
        private readonly HttpClient _httpClient;

        public HomeController(BlobService blobService, TableService tableService, QueueService queueService, FileService fileService, HttpClient httpClient)
        {
            _blobService = blobService;
            _tableService = tableService;
            _queueService = queueService;
            _fileService = fileService;
            _httpClient = httpClient;
        }

        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file != null)
            {
                using var stream = file.OpenReadStream();

                // Call the Azure Function for uploading blob
                var url = "https://st10263534.azurewebsites.net/api/UploadBlob?code=0nS8qtj9n8qaVqvZjH2Mh2TZ8HE1zjEuGKqJA9sdA1aYAzFumP3czg%3D%3D";
                var content = new StreamContent(stream);
                content.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);

                // Include parameters for containerName and blobName
                var response = await _httpClient.PostAsync($"{url}&containerName=product-images&blobName={file.FileName}", content);
                response.EnsureSuccessStatusCode(); // Throw if the request fails
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> AddCustomerProfile(CustomerProfile profile)
        {
            if (ModelState.IsValid)
            {
                var url = "https://st10263534.azurewebsites.net/api/StoreTableInfo?code=UdZ3M9o9nReZ0XwDZMuJhHs9BfBDUoCxl2z0NRvaQhMdAzFujwZNag%3D%3D";
                var jsonContent = new StringContent(JsonConvert.SerializeObject(profile), Encoding.UTF8, "application/json");

                // Call the Azure Function to store table information
                var response = await _httpClient.PostAsync(url, jsonContent);
                response.EnsureSuccessStatusCode(); // Throw if the request fails
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ProcessOrder(string orderId)
        {
            var url = "https://st10263534.azurewebsites.net/api/ProcessQueueMessage?code=bt0CE8oAdNjvyQY84H-HRSSbJMKK3iPc_T02uKrVeJMKAzFuGUOiqg%3D%3D";
            var jsonContent = new StringContent(JsonConvert.SerializeObject(new { orderId }), Encoding.UTF8, "application/json");

            // Call the Azure Function to process the order message
            var response = await _httpClient.PostAsync(url, jsonContent);
            response.EnsureSuccessStatusCode(); // Throw if the request fails

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> UploadContract(IFormFile file)
        {
            if (file != null)
            {
                using var stream = file.OpenReadStream();
                var url = "https://st10263534.azurewebsites.net/api/UploadFile?code=MlcZN6qiJngSI7xZXE_gKtYH4RP246ucdeveINrn4opQAzFukH76kg%3D%3D";

                var content = new StreamContent(stream);
                content.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);

                // Include parameters for shareName and fileName
                var response = await _httpClient.PostAsync($"{url}&shareName=contracts-logs&fileName={file.FileName}", content);
                response.EnsureSuccessStatusCode(); // Throw if the request fails
            }

            return RedirectToAction("Index");
        }
    }
}