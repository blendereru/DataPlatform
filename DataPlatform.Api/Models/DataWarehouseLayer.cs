namespace DataPlatform.Api.Models;

public enum DataWarehouseLayer
{
    /// <summary>
    /// Source system - external operational databases
    /// </summary>
    Source = 0,
    
    /// <summary>
    /// Operational Data Store - raw data from sources with minimal transformation
    /// </summary>
    Operational = 1,
    
    /// <summary>
    /// Staging/Integration Layer (T-1) - cleaned, standardized, with business rules applied
    /// </summary>
    Staging = 2,
    
    /// <summary>
    /// Data Warehouse Layer (T-2) - dimensional model with fact and dimension tables
    /// </summary>
    Warehouse = 3,
    
    /// <summary>
    /// Data Mart - department-specific aggregated views
    /// </summary>
    Mart = 4
}