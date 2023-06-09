﻿using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using InventoryManagementAppMVC.ViewModels;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Data;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using System.Net.Http.Headers;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using InventoryManagementAppMVC.Helper;

namespace InventoryManagementAppMVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AccountController(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
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

            var response = await _httpClient.GetAsync("api/Account/" + page + "/users");
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

        public IActionResult SignIn()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SignIn(SignInVM signInVM)
        {
            if (!ModelState.IsValid)
            {
                return View(signInVM);
            }

            // Send a POST request to the login API
            var response = await _httpClient.PostAsJsonAsync("api/Account/SignIn", signInVM);

            // Check if the request was successful
            if (response.IsSuccessStatusCode)
            {
                // Read the response content as a JWT token
                var token = await response.Content.ReadAsStringAsync();

                // Decode the JWT token
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);

                // Get the claims from the JWT token
                var claims = jwtToken.Claims.ToList();

                // Create a new ClaimsIdentity and add user information
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                // Create a new AuthenticationProperties object and set the access token
                var props = new AuthenticationProperties();
                props.StoreTokens(new List<AuthenticationToken>
                {
                    new AuthenticationToken { Name = "access_token", Value = token }
                });

                // Store the token in the authentication cookie and save it in the HTTP context
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), props);

                // Redirect the user to a protected page
                return RedirectToAction("Index", "Home");
            }
            else
            {
                // Handle unsuccessful login (e.g., display an error message)
                TempData["Error"] = "Wrong credentials. Please, try again";
                return View(signInVM);
            }
        }

        public IActionResult SignUp()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SignUp(SignUpCompany signUpCompany)
        {
            if (!ModelState.IsValid)
            {
                return View(signUpCompany);
            }

            CompanyVM companyVM = new CompanyVM()
            {
                Name = signUpCompany.CompanyName,
                Address = signUpCompany.Address,
                PhoneNumber = signUpCompany.PhoneNumber,
                Email = signUpCompany.Email,
                isDeleted = false
            };

            var responsePostCompany = await _httpClient.PostAsJsonAsync("api/Company", companyVM);

            if (!responsePostCompany.IsSuccessStatusCode)
            {
                TempData["Error"] = "Something went wrong";
                return View(signUpCompany);
            }

            var companyID = await responsePostCompany.Content.ReadAsStringAsync();

            SignUpVM signUpVM = new SignUpVM()
            {
                FirstName = signUpCompany.FirstName,
                LastName = signUpCompany.LastName,
                PhoneNumber = signUpCompany.PhoneNumber,
                Email = signUpCompany.Email,
                Password = signUpCompany.Password,
                ConfirmPassword = signUpCompany.ConfirmPassword,
                Role = UserRoles.Admin,
                CompanyID = int.Parse(companyID)
            };

            // Send a POST request to the login API
            var responsePostAccount = await _httpClient.PostAsJsonAsync("api/Account/SignUp", signUpVM);

            // Check if the request was successful
            if (!responsePostAccount.IsSuccessStatusCode)
            {
                TempData["Error"] = "Something went wrong";
                return View(signUpCompany);
            }

            TempData["Success"] = "You have sign up an account successfully";
            return RedirectToAction("SignIn");
        }

        public IActionResult SignUpEmployee()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SignUpEmployee(SignUpVM signUpVM)
        {
            if (!ModelState.IsValid)
            {
                return View(signUpVM);
            }

            var companyID = _httpContextAccessor.HttpContext?.User.GetUserCompanyID();

            signUpVM.CompanyID = int.Parse(companyID);
            signUpVM.Role = UserRoles.Staff;

            // Send a POST request to the login API
            var responsePostAccount = await _httpClient.PostAsJsonAsync("api/Account/SignUp", signUpVM);

            // Check if the request was successful
            if (!responsePostAccount.IsSuccessStatusCode)
            {
                TempData["Error"] = "Something went wrong";
                return View(signUpVM);
            }

            TempData["Success"] = "You have sign up an account successfully";
            return RedirectToAction("Index", new { page = 1 });
        }

        public async Task<IActionResult> SignOut()
        {
            await HttpContext.SignOutAsync();

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> AssignTruck(int page, string email)
        {
            ResponsePagination responsePage = new ResponsePagination();

            var accessToken = await HttpContext.GetTokenAsync("access_token");

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.GetAsync("api/Truck/" + page + "/assigntrucks");
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

            AssignTruckVM assignTruckVM = new AssignTruckVM()
            {
                responsePagination = responsePage,
                email = email,
                truckID = 0
            };

            return View(assignTruckVM);
        }

        [HttpPost]
        public async Task<IActionResult> AssignTruck(AssignTruckVM assignTruckVM)
        {
            AppUserVM appUserVM = new AppUserVM();

            var accessToken = await HttpContext.GetTokenAsync("access_token");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var responseGetAppUser = await _httpClient.GetAsync("api/Account/" + assignTruckVM.email + "/email");
            if (responseGetAppUser.IsSuccessStatusCode)
            {
                var apiResponse = await responseGetAppUser.Content.ReadAsStreamAsync();
                appUserVM = await JsonSerializer.DeserializeAsync<AppUserVM>(apiResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter() },
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
            }

            appUserVM.TruckID = assignTruckVM.truckID;
            appUserVM.Truck = null;

            var responsePutAppUser = await _httpClient.PutAsJsonAsync("api/Account/", appUserVM);
            var putAppUserContent = await responsePutAppUser.Content.ReadAsStringAsync();

            if (!responsePutAppUser.IsSuccessStatusCode)
            {
                TempData["Error"] = putAppUserContent;
                return RedirectToAction("Index", new { page = 1 });
            }

            TempData["Success"] = "You have assign truck #" + assignTruckVM.truckID + " for user " + appUserVM.FirstName;
            return RedirectToAction("Index", new { page = 1 });
        }
    }
}
