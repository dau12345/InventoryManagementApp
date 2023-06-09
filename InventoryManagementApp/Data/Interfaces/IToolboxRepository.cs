﻿using InventoryManagementApp.Data.Models;

namespace InventoryManagementApp.Data.Interfaces
{
    public interface IToolboxRepository
    {
        ICollection<Toolbox> GetToolboxes();
        Toolbox GetToolboxById(int toolboxID);
        bool ToolboxExists(int toolboxID);
        ICollection<ToolboxEquipment> GetToolboxEquipments(int toolboxID);
        bool CreateToolbox(Toolbox toolbox);
        bool UpdateToolbox(Toolbox toolbox);
        bool Save();
    }
}
