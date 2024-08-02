# SemesterTwo

Introduction

This guide provides step-by-step instructions to implement an ASP.NET Core MVC web application that utilizes Azure Storage Services. The application allows you to store customer profiles, product images, order processing details, and contracts/log files using Azure Tables, Blobs, Queues, and Files, respectively.

Prerequisites

Azure Subscription
Visual Studio 2022 or later
.NET 6 SDK
Azure Storage Account
Steps

1. Setting up the Project
Create a new ASP.NET Core MVC Project:
Open Visual Studio and create a new ASP.NET Core Web Application.
Select "ASP.NET Core Web App (Model-View-Controller)" template.
Add Necessary NuGet Packages:
Install the following NuGet packages:
Azure.Data.Tables
Azure.Storage.Blobs
Azure.Storage.Queues
Azure.Storage.Files.Shares

2. Configure Azure Storage
Add Connection String:
In appsettings.json, add your Azure Storage connection string:

{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "AzureStorage": {
    "ConnectionString": "YourAzureStorageConnectionString"
  }
}

4. Implement Services
BlobService.cs:

using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading.Tasks;
namespace SemesterTwo.Services
{
    public class BlobService
    {
        private readonly BlobServiceClient _blobServiceClient;

        public BlobService(IConfiguration configuration)
        {
            _blobServiceClient = new BlobServiceClient(configuration["AzureStorage:ConnectionString"]);
        }

        public async Task UploadBlobAsync(string containerName, string blobName, Stream content)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();
            var blobClient = containerClient.GetBlobClient(blobName);
            await blobClient.UploadAsync(content, true);
        }
    }
}

2. TableService.cs:

using Azure.Data.Tables;
using SemesterTwo.Models;
using System.Threading.Tasks;
namespace SemesterTwo.Services
{
    public class TableService
    {
        private readonly TableClient _tableClient;

        public TableService(IConfiguration configuration)
        {
            var connectionString = configuration["AzureStorage:ConnectionString"];
            var serviceClient = new TableServiceClient(connectionString);
            _tableClient = serviceClient.GetTableClient("CustomerProfiles");
            _tableClient.CreateIfNotExists();
        }

        public async Task AddEntityAsync(CustomerProfile profile)
        {
            await _tableClient.AddEntityAsync(profile);
        }
    }
}

3. QueueService.cs:

using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
namespace SemesterTwo.Services
{
    public class QueueService
    {
        private readonly QueueServiceClient _queueServiceClient;

        public QueueService(IConfiguration configuration)
        {
            _queueServiceClient = new QueueServiceClient(configuration["AzureStorage:ConnectionString"]);
        }

        public async Task SendMessageAsync(string queueName, string message)
        {
            var queueClient = _queueServiceClient.GetQueueClient(queueName);
            await queueClient.CreateIfNotExistsAsync();
            await queueClient.SendMessageAsync(message);
        }
    }
}

4. FileService.cs:

using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading.Tasks;
namespace SemesterTwo.Services
{
    public class FileService
    {
        private readonly ShareServiceClient _shareServiceClient;

        public FileService(IConfiguration configuration)
        {
            _shareServiceClient = new ShareServiceClient(configuration["AzureStorage:ConnectionString"]);
        }

        public async Task UploadFileAsync(string shareName, string fileName, Stream content)
        {
            var shareClient = _shareServiceClient.GetShareClient(shareName);
            await shareClient.CreateIfNotExistsAsync();
            var directoryClient = shareClient.GetRootDirectoryClient();
            var fileClient = directoryClient.GetFileClient(fileName);
            await fileClient.CreateAsync(content.Length);
            await fileClient.UploadAsync(content);
        }
    }
}

4. Implement Models
CustomerProfile.cs:

using Azure;
using Azure.Data.Tables;
using System;
namespace SemesterTwo.Models
{
    public class CustomerProfile : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        // Custom properties
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }

        public CustomerProfile()
        {
            PartitionKey = "CustomerProfile";
            RowKey = Guid.NewGuid().ToString();
        }
    }
}

5. Implement Controllers
HomeController.cs:

using Microsoft.AspNetCore.Mvc;
using SemesterTwo.Models;
using SemesterTwo.Services;
using System.Diagnostics;
using System.Threading.Tasks;
namespace SemesterTwo.Controllers
{
    public class HomeController : Controller
    {
        private readonly BlobService _blobService;
        private readonly TableService _tableService;
        private readonly QueueService _queueService;
        private readonly FileService _fileService;

        public HomeController(BlobService blobService, TableService tableService, QueueService queueService, FileService fileService)
        {
            _blobService = blobService;
            _tableService = tableService;
            _queueService = queueService;
            _fileService = fileService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file != null)
            {
                using var stream = file.OpenReadStream();
                await _blobService.UploadBlobAsync("product-images", file.FileName, stream);
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> AddCustomerProfile(CustomerProfile profile)
        {
            if (ModelState.IsValid)
            {
                await _tableService.AddEntityAsync(profile);
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ProcessOrder(string orderId)
        {
            await _queueService.SendMessageAsync("order-processing", $"Processing order {orderId}");
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> UploadContract(IFormFile file)
        {
            if (file != null)
            {
                using var stream = file.OpenReadStream();
                await _fileService.UploadFileAsync("contracts-logs", file.FileName, stream);
            }
            return RedirectToAction("Index");
        }
    }
}

6. Implement Views
Index.cshtml:

@model SemesterTwo.Models.CustomerProfile

<form asp-action="UploadImage" method="post" enctype="multipart/form-data">
    <input type="file" name="file" />
    <button type="submit">Upload Image</button>
</form>

<form asp-action="AddCustomerProfile" method="post">
    <input type="text" name="FirstName" placeholder="First Name" />
    <input type="text" name="LastName" placeholder="Last Name" />
    <input type="email" name="Email" placeholder="Email" />
    <input type="text" name="PhoneNumber" placeholder="Phone Number" />
    <button type="submit">Add Profile</button>
</form>

<form asp-action="ProcessOrder" method="post">
    <input type="text" name="orderId" placeholder="Order ID" />
    <button type="submit">Process Order</button>
</form>

<form asp-action="UploadContract" method="post" enctype="multipart/form-data">
    <input type="file" name="file" />
    <button type="submit">Upload Contract</button>
</form>

7. Register Services in Program.cs
Program.cs:

using SemesterTwo.Services;
namespace SemesterTwo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Register your custom services
            builder.Services.AddSingleton<BlobService>();
            builder.Services.AddSingleton<TableService>();
            builder.Services.AddSingleton<QueueService>();
            builder.Services.AddSingleton<FileService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
