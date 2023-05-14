﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using System.Text.Json;
using InventoryManagementAppMVC.ViewModels;
using InventoryManagementAppMVC.Helper;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InventoryManagementAppMVC.Controllers
{
    public class UsageLogController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UsageLogController(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            this._httpClient = httpClientFactory.CreateClient("myclient");
            this._httpContextAccessor = httpContextAccessor;
        }

        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Index(int page)
        {
            ResponsePagination responsePage = new ResponsePagination();

            var accessToken = await HttpContext.GetTokenAsync("access_token");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.GetAsync("api/UsageLog/" + page + "/usagelogs");
            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadAsStreamAsync();
                responsePage = await JsonSerializer.DeserializeAsync<ResponsePagination>(apiResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter() },
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
            }

            return View(responsePage);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            AppUserVM appUserVM = new AppUserVM();
            var accessToken = await HttpContext.GetTokenAsync("access_token");

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var userID = _httpContextAccessor.HttpContext?.User.GetUserId();
            var response = await _httpClient.GetAsync("api/Account/" + userID);
            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadAsStreamAsync();
                appUserVM = await JsonSerializer.DeserializeAsync<AppUserVM>(apiResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter() },
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
            }

            CreateUsageLogVM createUsageLogVM = new CreateUsageLogVM()
            {
                TruckID = appUserVM.TruckID,
                LicensePlate = appUserVM.Truck.LicensePlate,
                TruckStockItems = appUserVM.Truck.TruckStockItems.ToList()
            };

            foreach (var item in appUserVM.Truck.TruckStockItems)
            {
                createUsageLogVM.StockItemNames.Add((int)item.StockItemID, item.StockItem.Name);
                createUsageLogVM.StockItemQuantities.Add((int)item.StockItemID, 0);
                createUsageLogVM.TruckStockItemIDs.Add((int)item.StockItemID, item.TruckStockItemID);
            }

            return View(createUsageLogVM);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateUsageLogVM createUsageLogVM)
        {
            // Create master usagelog
            var userID = _httpContextAccessor.HttpContext?.User.GetUserId();
            var userName = _httpContextAccessor.HttpContext?.User.GetUserName();
            var companyID = _httpContextAccessor.HttpContext?.User.GetUserCompanyID();

            UsageLogVM usagelogVM = new UsageLogVM()
            {
                Date = DateTime.Now,
                TruckID = createUsageLogVM.TruckID,
                LicensePlate = createUsageLogVM.LicensePlate,
                AppUserID = userID,
                AppUserName = userName,
                CompanyID = int.Parse(companyID),
                isDeleted = false
            };
            
            //Post master usagelog then receive master usagelog id
            var accessToken = await HttpContext.GetTokenAsync("access_token");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var responsePostUsageLog = await _httpClient.PostAsJsonAsync("api/UsageLog", usagelogVM);
            var postUsageLogContent = await responsePostUsageLog.Content.ReadAsStringAsync();

            if (!responsePostUsageLog.IsSuccessStatusCode)
            {
                TempData["Error"] = postUsageLogContent;
                return RedirectToAction("MyUsageLog", new { page = 1 });
            }

            //Create detail usagelogs
            List<DetailUsageLogVM> detailUsageLogVMs = new List<DetailUsageLogVM>();

            foreach (var item in createUsageLogVM.StockItemQuantities)
            {
                int stockItemID = item.Key;
                int quantity = item.Value;

                if (quantity > 0)
                {
                    DetailUsageLogVM newDetailUsageLog = new DetailUsageLogVM()
                    {
                        StockItemID = stockItemID,
                        StockItemName = createUsageLogVM.StockItemNames[stockItemID],
                        Quantity = quantity,
                        UsageLogID = int.Parse(postUsageLogContent),
                        CompanyID = int.Parse(companyID),
                        isDeleted = false
                    };

                    detailUsageLogVMs.Add(newDetailUsageLog);

                    //Get truck stock item
                    var truckStockItemID = createUsageLogVM.TruckStockItemIDs[stockItemID];
                    TruckStockItemVM responseTruckStockItemVM = new TruckStockItemVM();

                    var response = await _httpClient.GetAsync("api/TruckStockItem/" + truckStockItemID);
                    if (response.IsSuccessStatusCode)
                    {
                        var apiResponse = await response.Content.ReadAsStreamAsync();
                        responseTruckStockItemVM = await JsonSerializer.DeserializeAsync<TruckStockItemVM>(apiResponse, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                        });
                    }
                    responseTruckStockItemVM.QuantityInTruck -= quantity;

                    //Put truck stock item to update quantity in truck
                    var responsePutTruckStockItem = await _httpClient.PutAsJsonAsync("api/TruckStockItem/" + truckStockItemID, responseTruckStockItemVM);
                    var putTruckStockItemContent = await responsePutTruckStockItem.Content.ReadAsStringAsync();

                    if (!responsePutTruckStockItem.IsSuccessStatusCode)
                    {
                        TempData["Error"] = putTruckStockItemContent;
                        return RedirectToAction("MyUsageLog", new { page = 1 });
                    }
                }
            }

            if (detailUsageLogVMs.Count() < 1)
            {
                TempData["Error"] = "To create usagelog you must change quantity of at least 1 item";
                return View();
            }

            //Post list of detail usagelogs
            var responsePostDetailUsageLog = await _httpClient.PostAsJsonAsync("api/DetailUsageLog", detailUsageLogVMs);
            var postDetailUsageLogContent = await responsePostDetailUsageLog.Content.ReadAsStringAsync();

            if (!responsePostDetailUsageLog.IsSuccessStatusCode)
            {
                TempData["Error"] = postDetailUsageLogContent;
                return RedirectToAction("MyUsageLog", new { page = 1 });
            }

            TempData["Success"] = "Create new usage log successfully";
            return RedirectToAction("MyUsageLog", new { page = 1 });
        }

        [HttpGet]
        public async Task<IActionResult> MyUsageLog(int page)
        {
            ResponsePagination responsePage = new ResponsePagination();

            var accessToken = await HttpContext.GetTokenAsync("access_token");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var userID = _httpContextAccessor.HttpContext?.User.GetUserId();
            var response = await _httpClient.GetAsync("api/UsageLog/" + page + "/myusagelogs/" + userID);
            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadAsStreamAsync();
                responsePage = await JsonSerializer.DeserializeAsync<ResponsePagination>(apiResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter() },
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
            }

            return View(responsePage);
        }
    }
}
