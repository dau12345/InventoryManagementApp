﻿using InventoryManagementAppMVC.Enum;

namespace InventoryManagementAppMVC.ViewModels
{
    public class EquipmentVM
    {
        public int EquipmentID { get; set; }
        public string Name { get; set; }
        public int Quantity { get; set; }
        public EquipmentType Type { get; set; }
        public QualityState QualityState { get; set; }
        public int? CompanyID { get; set; }
        public bool isDeleted { get; set; }
    }
}