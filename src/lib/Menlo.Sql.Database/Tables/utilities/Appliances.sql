CREATE TABLE [utilities].[Appliances]
(
    [ID]            INT NOT NULL,
    [Name]          VARCHAR(255) NOT NULL,
    [Description]   NVARCHAR(2000) NOT NULL,
    [PurchaseDate]  DATETIME NOT NULL,

    CONSTRAINT [PK_Appliances_Id] PRIMARY KEY ([ID])
)