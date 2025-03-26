using Azure.Provisioning.Sql;
using Microsoft.Extensions.Options;

var builder = DistributedApplication.CreateBuilder(args);

var sqlService = builder.AddAzureSqlServer("sqlService")
    .ConfigureInfrastructure(infra =>
    {
        var azureResources = infra.GetProvisionableResources();
        var azureDb = azureResources.OfType<SqlDatabase>().Single();
        azureDb.Sku = new SqlSku() { Name = "GP_S_Gen5_2" };
    })
    .RunAsContainer();

var articleDb = sqlService.AddDatabase("articlesDB");

var articleDAC = builder.AddSqlProject<Projects.ProPulse_ArticlesDB>("propulse-articlesdb")
    .WithConfigureDacDeployOptions(options => options.AllowTableRecreation = false)
    .WithReference(articleDb)
    .WaitFor(articleDb);

builder.AddDataAPIBuilder("dab")
    .WithReference(articleDb)
    .WaitFor(articleDb)
    .WaitForCompletion(articleDAC);

builder.Build().Run();
