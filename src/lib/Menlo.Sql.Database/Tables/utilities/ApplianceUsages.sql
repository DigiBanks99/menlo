CREATE TABLE [utilities].[ApplianceUsages]
(
    [ApplianceID] INT NOT NULL,
    [ElectricityUsageID] INT NOT NULL,
    [HoursOfUse] DECIMAL(18, 2) NOT NULL,

    CONSTRAINT [PK_ApplianceUsages] PRIMARY KEY ([ApplianceID], [ElectricityUsageID]),
    CONSTRAINT [FK_ApplianceUsages_Appliances_ApplianceId] FOREIGN KEY ([ApplianceID]) REFERENCES [utilities].[Appliances] ([ID]),
    CONSTRAINT [FK_ApplianceUsages_ElectricityUsages_ElectricityUsageId] FOREIGN KEY ([ElectricityUsageID]) REFERENCES [utilities].[ElectricityUsages] ([ID])
)
