using Azure.Provisioning.Sql;

var builder = DistributedApplication.CreateBuilder(args);

// See https://github.com/CommunityToolkit/Aspire/issues/529
// var sqlService = builder.AddAzureSqlServer("sqlService")
//     .ConfigureInfrastructure(infra =>
//     {
//         var azureResources = infra.GetProvisionableResources();
//         var azureDb = azureResources.OfType<SqlDatabase>().Single();
//         azureDb.Sku = new SqlSku() { Name = "GP_S_Gen5_2" };
//     })
//     .RunAsContainer(configure => configure.WithLifetime(ContainerLifetime.Persistent));

// Until CommunityToolkit/Aspire#529 is fixed, use a container.
var sqlService = builder.AddSqlServer("sqlService", port: 1435)
    .WithLifetime(ContainerLifetime.Persistent);

var articlesDb = sqlService.AddDatabase("propulse-articles");

var articlesDacPac = builder.AddSqlProject<Projects.ProPulse_ArticlesDB>("articlesdb-dacpac")
    .WithConfigureDacDeployOptions((options) => options.AllowTableRecreation = false)
    .WithReference(articlesDb)
    .WaitFor(articlesDb);

var identityDb = sqlService.AddDatabase("propulse-identity");

var identityService = builder.AddProject<Projects.ProPulse_IdentityService>("identity-service")
    .WithReference(identityDb, "IdentityConnection")
    .WithExternalHttpEndpoints()
    .WaitFor(identityDb);

builder.AddDataAPIBuilder("dab")
    .WithReference(articlesDb, "DefaultConnection")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithExternalHttpEndpoints()
    .WaitFor(articlesDb)
    .WaitForCompletion(articlesDacPac);

builder.Build().Run();
