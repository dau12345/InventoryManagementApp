﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using System.Text.Json;
using InventoryManagementAppMVC.ViewModels;
using InventoryManagementAppMVC.Helper;

namespace InventoryManagementAppMVC.Controllers
{
    public class TruckController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TruckController(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
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

            var response = await _httpClient.GetAsync("api/Truck/" + page + "/trucks");
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
        public async Task<IActionResult> MyTruck()
        {
            TruckVM truckVM = new TruckVM();
            var accessToken = await HttpContext.GetTokenAsync("access_token");

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var truckID = _httpContextAccessor.HttpContext?.User.GetUserTruckID();
            var response = await _httpClient.GetAsync("api/Truck/" + truckID);
            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadAsStreamAsync();
                truckVM = await JsonSerializer.DeserializeAsync<TruckVM>(apiResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter() },
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
            }

            return View(truckVM);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(TruckVM truckVM)
        {
            if (!ModelState.IsValid)
            {
                return View(truckVM);
            }

            var companyID = _httpContextAccessor.HttpContext?.User.GetUserCompanyID();

            var accessToken = await HttpContext.GetTokenAsync("access_token");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            //Post toolbox

            ToolboxVM toolboxVM = new ToolboxVM()
            {
                CompanyID = int.Parse(companyID),
                isDeleted = false,
            };

            var responsePostToolbox = await _httpClient.PostAsJsonAsync("api/Toolbox", toolboxVM);
            if (!responsePostToolbox.IsSuccessStatusCode)
            {
                TempData["Error"] = "Create toolbox failed";
                return View();
            }

            var toolboxID = await responsePostToolbox.Content.ReadAsStringAsync();

            //Post truck

            truckVM.ToolboxID = int.Parse(toolboxID);
            truckVM.CompanyID = int.Parse(companyID);
            truckVM.isDeleted = false;

            var responsePostTruck = await _httpClient.PostAsJsonAsync("api/Truck", truckVM);

            if (!responsePostTruck.IsSuccessStatusCode)
            {
                TempData["Error"] = await responsePostTruck.Content.ReadAsStringAsync();
                return View(truckVM);
            }

            var truckID = await responsePostTruck.Content.ReadAsStringAsync();

            //Post truck stock item

            List<StockItemVM> stockItemVMs = new List<StockItemVM>();

            var stockItemVMsResponse = await _httpClient.GetAsync("api/StockItem");
            if (stockItemVMsResponse.IsSuccessStatusCode)
            {
                var apiResponse = await stockItemVMsResponse.Content.ReadAsStreamAsync();
                stockItemVMs = await JsonSerializer.DeserializeAsync<List<StockItemVM>>(apiResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter() },
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
            }

            List<TruckStockItemVM> truckStockItemVMs = new List<TruckStockItemVM>();

            foreach (var item in stockItemVMs)
            {
                TruckStockItemVM truckStockItemVM = new TruckStockItemVM()
                {
                    TruckID = int.Parse(truckID),
                    StockItemID = item.StockItemID,
                    QuantityInTruck = 0,
                    CompanyID = int.Parse(companyID),
                    isDeleted = false
                };

                truckStockItemVMs.Add(truckStockItemVM);
            }

            var responsePostTruckStockItem = await _httpClient.PostAsJsonAsync("api/TruckStockItem", truckStockItemVMs);
            var postTruckStockItemContent = await responsePostTruckStockItem.Content.ReadAsStringAsync();

            if (!responsePostTruckStockItem.IsSuccessStatusCode)
            {
                TempData["Error"] = postTruckStockItemContent;
                return RedirectToAction("Index", new { page = 1 });
            }

            //Post toolbox equipment

            List<EquipmentVM> equipmentVMs = new List<EquipmentVM>();

            var equipmentVMsResponse = await _httpClient.GetAsync("api/Equipment");
            if (equipmentVMsResponse.IsSuccessStatusCode)
            {
                var apiResponse = await equipmentVMsResponse.Content.ReadAsStreamAsync();
                equipmentVMs = await JsonSerializer.DeserializeAsync<List<EquipmentVM>>(apiResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter() },
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
            }

            List<ToolboxEquipmentVM> toolboxEquipmentVMs = new List<ToolboxEquipmentVM>();

            foreach (var item in equipmentVMs)
            {
                ToolboxEquipmentVM toolboxEquipmentVM = new ToolboxEquipmentVM()
                {
                    ToolboxID = int.Parse(toolboxID),
                    EquipmentID = item.EquipmentID,
                    QuantityInToolbox = 0,
                    CompanyID = int.Parse(companyID),
                    isDeleted = false
                };

                toolboxEquipmentVMs.Add(toolboxEquipmentVM);
            }

            var responsePostToolboxEqipment = await _httpClient.PostAsJsonAsync("api/ToolboxEquipment", toolboxEquipmentVMs);
            var postToolboxEquipmentContent = await responsePostTruckStockItem.Content.ReadAsStringAsync();

            if (!responsePostToolboxEqipment.IsSuccessStatusCode)
            {
                TempData["Error"] = postToolboxEquipmentContent;
                return RedirectToAction("Index", new { page = 1 });
            }

            TempData["Success"] = "You have been created a new truck";
            return RedirectToAction("Index", new { page = 1 });
        }

        [HttpGet]
        public async Task<IActionResult> Update(int id)
        {
            TruckVM responseTruck = new TruckVM();

            var accessToken = await HttpContext.GetTokenAsync("access_token");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.GetAsync("api/Truck/" + id);
            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadAsStreamAsync();
                responseTruck = await JsonSerializer.DeserializeAsync<TruckVM>(apiResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
            }

            return View(responseTruck);
        }

        [HttpPost]
        public async Task<IActionResult> Update(TruckVM truckVM)
        {
            if (!ModelState.IsValid)
            {
                return View(truckVM);
            }

            var accessToken = await HttpContext.GetTokenAsync("access_token");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var responsePut = await _httpClient.PutAsJsonAsync("api/Truck/" + truckVM.TruckID, truckVM);
            if (!responsePut.IsSuccessStatusCode)
            {
                TempData["Error"] = await responsePut.Content.ReadAsStringAsync();
                return View(truckVM);
            }

            TempData["Success"] = "Update truck successfully";
            return RedirectToAction("Index", new { page = 1 });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            TruckVM responseTruck = new TruckVM();

            var accessToken = await HttpContext.GetTokenAsync("access_token");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.GetAsync("api/Truck/" + id);
            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadAsStreamAsync();
                responseTruck = await JsonSerializer.DeserializeAsync<TruckVM>(apiResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
            }

            responseTruck.isDeleted = true;

            var responsePut = await _httpClient.PutAsJsonAsync("api/Truck/" + responseTruck.TruckID, responseTruck);
            if (!responsePut.IsSuccessStatusCode)
            {
                TempData["Error"] = await responsePut.Content.ReadAsStringAsync();
                return RedirectToAction("Index", new { page = 1 });
            }

            TempData["Success"] = "Delete truck successfully";
            return RedirectToAction("Index", new { page = 1 });
        }
    }
}
