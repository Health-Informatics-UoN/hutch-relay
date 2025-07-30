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
using Microsoft.FeatureManagement;
using Serilog;

namespace Hutch.Relay.Startup.Web;

public static class ConfigureWebServices
{
  public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
  {
    // Feature Management from config sections with "Enable" keys
    builder.Configuration.DeclareSectionFeatures([
      TaskApiPollingOptions.Section,
      RelayBeaconOptions.Section]);
    builder.Services.AddFeatureManagement();

    // Logging
    builder.Services.AddSerilog((services, lc) => lc
      .ReadFrom.Configuration(builder.Configuration)
      .ReadFrom.Services(services)
      .Enrich.FromLogContext());

    var connectionString = builder.Configuration.GetConnectionString("Default");
    builder.Services.AddDbContext<ApplicationDbContext>(o => { o.UseNpgsql(connectionString); });
    builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection(DatabaseOptions.Section));

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
      .Configure<TaskApiPollingOptions>(builder.Configuration.GetSection(TaskApiPollingOptions.Section))
      .Configure<ApiClientOptions>(builder.Configuration.GetSection(TaskApiPollingOptions.Section))
      .AddHttpClient()
      .AddTransient<ITaskApiClient, TaskApiClient>()
      .AddScoped<UpstreamTaskPoller>()
      .AddTransient<ResultsService>();

    // Task Queue
    builder.Services
      .Configure<RelayTaskQueueOptions>(builder.Configuration.GetSection(RelayTaskQueueOptions.Section))
      .AddTransient<IRelayTaskQueue, RabbitRelayTaskQueue>(); // TODO: Azure / Other native queues

    // App Initialisation Services
    builder.Services
      .Configure<DownstreamUsersOptions>(builder.Configuration.GetSection(DownstreamUsersOptions.Section))
      .AddTransient<WebInitialisationService>()
      .AddTransient<DeclarativeConfigService>()
      .AddTransient<DbManagementService>();

    // Other App Services
    builder.Services
      .AddTransient<IDownstreamTaskService, DownstreamTaskService>()
      .AddTransient<IRelayTaskService, RelayTaskService>()
      .AddTransient<ISubNodeService, SubNodeService>();

    // Obfuscation
    builder.Services
      .Configure<ObfuscationOptions>(builder.Configuration.GetSection(ObfuscationOptions.Section))
      .AddTransient<IObfuscator, Obfuscator>();

    // Aggregators
    builder.Services
      .AddKeyedTransient<IQueryResultAggregator, AvailabilityAggregator>(nameof(AvailabilityAggregator))
      .AddKeyedTransient<IQueryResultAggregator, GenericDistributionAggregator>(nameof(GenericDistributionAggregator))
      .AddKeyedTransient<IQueryResultAggregator, DemographicsDistributionAggregator>(nameof(DemographicsDistributionAggregator));

    // Beacon
    var isBeaconEnabled = builder.Configuration.IsSectionEnabled(RelayBeaconOptions.Section);
    builder.Services
      .Configure<BaseBeaconOptions>(builder.Configuration.GetSection(RelayBeaconOptions.Section))
      .Configure<RelayBeaconOptions>(builder.Configuration.GetSection(RelayBeaconOptions.Section));
    if (isBeaconEnabled)
      builder.Services.AddTransient<IFilteringTermsService, FilteringTermsService>();

    // Hosted Services
    var isUpstreamTaskApiEnabled = builder.Configuration.IsSectionEnabled(TaskApiPollingOptions.Section);
    if (isUpstreamTaskApiEnabled)
      builder.Services
        .AddHostedService<BackgroundUpstreamTaskPoller>()
        .AddScoped<ScopedTaskHandler>();

    builder.Services.AddHostedService<TaskCompletionHostedService>();

    // Monitoring
    builder.Services.Configure<MonitoringOptions>(builder.Configuration.GetSection(MonitoringOptions.Section));
    builder.Services.AddHealthChecks()
      .AddDbContextCheck<ApplicationDbContext>();

    return builder;
  }
}
