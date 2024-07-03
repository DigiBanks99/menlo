CREATE TABLE [utilities].[ElectricityUsages]
(
    [Id]    INT NOT NULL,
    [Date]  DATETIMEOFFSET NOT NULL,
    [Units] DECIMAL(18, 2) NOT NULL,

    CONSTRAINT [PK_ElectricityUsages] PRIMARY KEY ([ID])
)
