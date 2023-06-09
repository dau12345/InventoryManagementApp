﻿using InventoryManagementApp.Data;
using InventoryManagementApp.Data.Repository;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;

namespace InventoryManagementApp.Helper
{
    public class TenantMiddleware
    {
        private readonly RequestDelegate _next;

        public TenantMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, DataContext userContext)
        {
            if (context.Request.Headers.TryGetValue("Authorization", out var authHeader) &&
            authHeader.Count > 0 && authHeader[0].StartsWith("Bearer "))
            {
                var token = authHeader[0].Substring("Bearer ".Length).Trim();
                var handler = new JwtSecurityTokenHandler();
                var decodedToken = handler.ReadJwtToken(token);
                var tenantId = decodedToken.Claims.FirstOrDefault(c => c.Type == "CompanyID")?.Value;
                userContext.TenantID = int.Parse(tenantId);
            }

            await _next(context);
        }
    }

}
