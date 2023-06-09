﻿using InventoryManagementApp.Data.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManagementApp.Data.Models
{
    public class UsageLog : ITenantEntity
    {
        public int UsageLogID { get; set; }
        public DateTime Date { get; set; }
        public int? TruckID { get; set; }
        public string? LicensePlate { get; set; }
        public Truck? Truck { get; set; }
        [ForeignKey("AppUser")]
        public string? AppUserID { get; set; }
        public string? AppUserName { get; set; }
        public AppUser? AppUser { get; set; }
        public int? CompanyID { get; set; }
        public Company? Company { get; set; }
        public bool isDeleted { get; set; }

        public ICollection<DetailUsageLog>? DetailUsageLogs { get; set; }
    }
}
