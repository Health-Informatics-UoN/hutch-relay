using Hutch.Rackit;
using Hutch.Rackit.TaskApi;
using Hutch.Rackit.TaskApi.Contracts;
using Hutch.Relay.Auth.Basic;
using Hutch.Relay.Config;
using Hutch.Relay.Config.Beacon;
using Hutch.Relay.Constants;
using Hutch.Relay.Data;
using Hutch.Relay.Data.Entities;
using Hutch.Relay.Services;
using Hutch.Relay.Services.Contracts;
using Hutch.Relay.Services.Hosted;
using Hutch.Relay.Services.JobResultAggregators;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Hutch.Relay.Startup.Web;

public static class ConfigureWebServices
{
  public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
  {
    // Logging
    builder.Services.AddSerilog((services, lc) => lc
      .ReadFrom.Configuration(builder.Configuration)
      .ReadFrom.Services(services)
      .Enrich.FromLogContext());

    var connectionString = builder.Configuration.GetConnectionString("Default");
    builder.Services.AddDbContext<ApplicationDbContext>(o => { o.UseNpgsql(connectionString); });
    builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection("Database"));

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

    // Upstream Task API
    builder.Services
      .Configure<TaskApiPollingOptions>(builder.Configuration.GetSection("UpstreamTaskApi"))
      .Configure<ApiClientOptions>(builder.Configuration.GetSection("UpstreamTaskApi"))
      .AddHttpClient()
      .AddTransient<ITaskApiClient, TaskApiClient>()
      .AddScoped<UpstreamTaskPoller>()
      .AddTransient<ResultsService>();

    // Task Queue
    builder.Services
      .Configure<RelayTaskQueueOptions>(builder.Configuration.GetSection("RelayTaskQueue"))
      .AddTransient<IRelayTaskQueue, RabbitRelayTaskQueue>(); // TODO: Azure / Other native queues

    // App Initialisation Services
    builder.Services
      .Configure<DownstreamUsersOptions>(builder.Configuration.GetSection("DownstreamUsers"))
      .AddTransient<WebInitialisationService>()
      .AddTransient<DeclarativeConfigService>()
      .AddTransient<DbManagementService>();

    // Other App Services
    builder.Services
      .AddTransient<IRelayTaskService, RelayTaskService>()
      .AddTransient<ISubNodeService, SubNodeService>();

    // Obfuscation
    builder.Services
      .Configure<ObfuscationOptions>(builder.Configuration.GetSection("Obfuscation"))
      .AddTransient<IObfuscator, Obfuscator>();

    // Aggregators
    builder.Services
      .AddKeyedTransient<IQueryResultAggregator, AvailabilityAggregator>(nameof(AvailabilityAggregator))
      .AddKeyedTransient<IQueryResultAggregator, GenericDistributionAggregator>(nameof(GenericDistributionAggregator))
      .AddKeyedTransient<IQueryResultAggregator, DemographicsDistributionAggregator>(nameof(DemographicsDistributionAggregator));

    // Beacon
    builder.Services
      .Configure<BaseBeaconOptions>(builder.Configuration.GetSection("Beacon"))
      .Configure<RelayBeaconOptions>(builder.Configuration.GetSection("Beacon"));

    // Hosted Services
    var isUpstreamTaskApiEnabled = builder.Configuration.GetSection("UpstreamTaskApi").GetValue<bool>("Enable");
    if (isUpstreamTaskApiEnabled)
      builder.Services
        .AddHostedService<BackgroundUpstreamTaskPoller>()
        .AddScoped<ScopedTaskHandler>();

    builder.Services.AddHostedService<TaskCompletionHostedService>();

    // Monitoring
    builder.Services.Configure<MonitoringOptions>(builder.Configuration.GetSection("Monitoring"));
    builder.Services.AddHealthChecks()
      .AddDbContextCheck<ApplicationDbContext>();

    return builder;
  }
}
