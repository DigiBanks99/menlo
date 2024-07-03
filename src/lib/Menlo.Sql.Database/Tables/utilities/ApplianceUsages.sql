CREATE TABLE [utilities].[ApplianceUsages]
(
    [ApplianceId] INT NOT NULL,
    [ElectricityUsageId] INT NOT NULL,
    [HoursOfUse] DECIMAL(18, 2) NOT NULL,

    CONSTRAINT [PK_ApplianceUsages] PRIMARY KEY ([ApplianceId], [ElectricityUsageId]),
    CONSTRAINT [FK_ApplianceUsages_Appliances_ApplianceId] FOREIGN KEY ([ApplianceId]) REFERENCES [utilities].[Appliances] ([Id]),
    CONSTRAINT [FK_ApplianceUsages_ElectricityUsages_ElectricityUsageId] FOREIGN KEY ([ElectricityUsageId]) REFERENCES [utilities].[ElectricityUsages] ([Id])
)
