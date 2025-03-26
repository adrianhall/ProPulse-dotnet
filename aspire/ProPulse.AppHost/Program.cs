using Azure.Provisioning.Sql;

var builder = DistributedApplication.CreateBuilder(args);

var sqlService = builder.AddAzureSqlServer("sqlService")
    .ConfigureInfrastructure(infra =>
    {
        var azureResources = infra.GetProvisionableResources();
        var azureDb = azureResources.OfType<SqlDatabase>().Single();
        azureDb.Sku = new SqlSku() { Name = "GP_S_Gen5_2" };
    })
    .RunAsContainer(configure => configure.WithLifetime(ContainerLifetime.Persistent));

var articlesDb = sqlService.AddDatabase("propulse-articles");

var articlesDacPac = builder.AddSqlProject<Projects.ProPulse_ArticlesDB>("articlesdb-dacpac")
    .WithConfigureDacDeployOptions((options) => options.AllowTableRecreation = false)
    .WithReference(articlesDb)
    .WaitFor(articlesDb);

builder.AddDataAPIBuilder("dab")
    .WithReference(articlesDb, "DefaultConnection")
    .WaitFor(articlesDb)
    .WaitForCompletion(articlesDacPac);

builder.Build().Run();
