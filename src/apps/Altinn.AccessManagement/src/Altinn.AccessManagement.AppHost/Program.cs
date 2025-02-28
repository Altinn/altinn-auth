var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Altinn_AccessManagement_Api_Enduser>("altinn-accessmanagement-api-enduser");
builder.AddProject<Projects.Altinn_AccessManagement>("altinn-accessmanagement");

builder.AddProject<Projects.Altinn_AccessManagement_Api_ServiceOwner>("altinn-accessmanagement-api-serviceowner");

builder.AddProject<Projects.Altinn_AccessManagement_Api_Metadata>("altinn-accessmanagement-api-metadata");

//// builder.AddProject<Projects.Altinn_AccessMgmt_AutoApi>("altinn-accessmgmt-autoapi");

builder.Build().Run();
