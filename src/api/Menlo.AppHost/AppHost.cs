var builder = DistributedApplication.CreateBuilder(args);

// Add Menlo.Api project as a distributed service
var api = builder.AddProject<Projects.Menlo_Api>("menlo-api");

builder.Build().Run();
