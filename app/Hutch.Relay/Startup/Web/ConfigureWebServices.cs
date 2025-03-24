using Hutch.Rackit;
using Hutch.Rackit.TaskApi;
using Hutch.Rackit.TaskApi.Contracts;
using Hutch.Relay.Auth.Basic;
using Hutch.Relay.Config;
using Hutch.Relay.Constants;
using Hutch.Relay.Data;
using Hutch.Relay.Data.Entities;
using Hutch.Relay.Services;
using Hutch.Relay.Services.Contracts;
using Hutch.Relay.Services.Hosted;
using Hutch.Relay.Services.JobResultAggregators;
using Microsoft.EntityFrameworkCore;

namespace Hutch.Relay.Startup.Web;

public static class ConfigureWebServices
{
  public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
  {
    var connectionString = builder.Configuration.GetConnectionString("Default");
    builder.Services.AddDbContext<ApplicationDbContext>(o => { o.UseNpgsql(connectionString); });
    
    builder.Services.AddIdentityCore<RelayUser>(DefaultIdentityOptions.Configure)
      .AddEntityFrameworkStores<ApplicationDbContext>();
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddAuthentication("Basic")
      .AddScheme<BasicAuthSchemeOptions, BasicAuthHandler>("Basic", opts => 
      {
        opts.Realm = "relay";
      });
    builder.Services.AddSwaggerGen(o => o.UseOneOfForPolymorphism());

    // Headless Mode
    builder.Services
      .Configure<HeadlessModeOptions>(builder.Configuration.GetSection("HeadlessMode"));
    
    var headlessModeOptions = builder.Configuration.GetSection("HeadlessMode").Get<HeadlessModeOptions>();
    
    if (!headlessModeOptions?.IsHeadlessModeEnabled ?? false)
    {
      // Upstream Task API
      builder.Services
        .Configure<ApiClientOptions>(builder.Configuration.GetSection("UpstreamTaskApi"))
        .AddHttpClient()
        .AddTransient<ITaskApiClient, TaskApiClient>()
        .AddScoped<UpstreamTaskPoller>()
        .AddTransient<ResultsService>();

      // Hosted Services
      builder.Services.AddHostedService<BackgroundUpstreamTaskPoller>();
      builder.Services.AddHostedService<TaskCompletionHostedService>();
    }

    // Task Queue
    builder.Services
      .Configure<RelayTaskQueueOptions>(builder.Configuration.GetSection("RelayTaskQueue"))
      .AddTransient<IRelayTaskQueue, RabbitRelayTaskQueue>(); // TODO: Azure / Other native queues

    // Other App Services
    builder.Services
      .AddTransient<IRelayTaskService, RelayTaskService>()
      .AddTransient<ISubNodeService, SubNodeService>()
      .AddTransient<DbManagementService>();
    
    // Obfuscation
    builder.Services
      .Configure<ObfuscationOptions>(builder.Configuration.GetSection(("Obfuscation")))
      .AddTransient<IObfuscator, Obfuscator>();
    
    // Aggregators
    builder.Services
      .AddKeyedTransient<IQueryResultAggregator,AvailabilityAggregator>(nameof(AvailabilityAggregator))
      .AddKeyedTransient<IQueryResultAggregator,GenericDistributionAggregator>(nameof(GenericDistributionAggregator))
      .AddKeyedTransient<IQueryResultAggregator,DemographicsDistributionAggregator>(nameof(DemographicsDistributionAggregator));

    return builder;
  }
}
