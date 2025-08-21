using Hutch.Rackit;
using Hutch.Rackit.TaskApi;
using Hutch.Rackit.TaskApi.Contracts;
using Hutch.Relay.Auth.Basic;
using Hutch.Relay.Config;
using Hutch.Relay.Config.Beacon;
using Hutch.Relay.Config.Helpers;
using Hutch.Relay.Constants;
using Hutch.Relay.Data;
using Hutch.Relay.Data.Entities;
using Hutch.Relay.Services;
using Hutch.Relay.Services.Contracts;
using Hutch.Relay.Services.Hosted;
using Hutch.Relay.Services.JobResultAggregators;
using Hutch.Relay.Services.RabbitQueues;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using RabbitMQ.Client;
using Serilog;

namespace Hutch.Relay.Startup.Web;

public static class ConfigureWebServices
{
  public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder b)
  {
    // Feature Management
    b.Configuration.DeclareOptionsModelFeatures([
      typeof(TaskApiPollingOptions),
      typeof(RelayBeaconOptions)
    ]);
    b.Services.AddFeatureManagement();

    // Logging
    b.Services.AddSerilog((services, lc) => lc
      .ReadFrom.Configuration(b.Configuration)
      .ReadFrom.Services(services)
      .Enrich.FromLogContext());

    var connectionString = b.Configuration.GetConnectionString("Default");
    b.Services.AddDbContext<ApplicationDbContext>(o => { o.UseNpgsql(connectionString); });
    b.Services.Configure<DatabaseOptions>();

    b.Services.AddIdentityCore<RelayUser>(DefaultIdentityOptions.Configure)
      .AddEntityFrameworkStores<ApplicationDbContext>();
    b.Services.AddControllers().AddJsonOptions(DefaultJsonOptions.Configure);
    b.Services.AddEndpointsApiExplorer();
    b.Services.AddAuthentication("Basic")
      .AddScheme<BasicAuthSchemeOptions, BasicAuthHandler>("Basic", opts => { opts.Realm = "relay"; });
    b.Services.AddSwaggerGen(o => o.UseOneOfForPolymorphism());

    // Upstream Task API
    b.Services
      .Configure<TaskApiPollingOptions>()
      .Configure<ApiClientOptions>(b.Configuration.GetSection<TaskApiPollingOptions>())
      .AddHttpClient()
      .AddTransient<ITaskApiClient, TaskApiClient>()
      .AddScoped<UpstreamTaskPoller>()
      .AddTransient<ResultsService>();

    // Task Queue
    // TODO: Azure / Other native queues
    b.Services
      .Configure<RelayTaskQueueOptions>()
      .AddSingleton<IConnectionFactory>(s =>
      {
        var queueConnectionString = s
          .GetRequiredService<IOptions<RelayTaskQueueOptions>>()
          .Value.ConnectionString;
        return new ConnectionFactory
        {
          Uri = new(queueConnectionString),
        };
      })
      .AddSingleton<RabbitConnectionManager>()
      .AddSingleton<IRabbitConnectionManager, RabbitConnectionManager>(s =>
        s.GetRequiredService<RabbitConnectionManager>())
      .AddSingleton<IQueueConnectionManager, RabbitConnectionManager>(s =>
        s.GetRequiredService<RabbitConnectionManager>())
      .AddTransient<IDownstreamTaskQueue, RabbitDownstreamTaskQueue>()
      .AddTransient<IBeaconResultsQueue, RabbitBeaconResultsQueue>();

    // App Initialisation Services
    b.Services
      .Configure<DownstreamUsersOptions>()
      .AddTransient<WebInitialisationService>()
      .AddTransient<DeclarativeConfigService>()
      .AddTransient<DbManagementService>();

    // Other App Services
    b.Services
      .AddTransient<IDownstreamTaskService, DownstreamTaskService>()
      .AddTransient<IRelayTaskService, RelayTaskService>()
      .AddTransient<ISubNodeService, SubNodeService>();

    // Obfuscation
    b.Services
      .Configure<ObfuscationOptions>()
      .AddTransient<IObfuscator, Obfuscator>();

    // Aggregators
    b.Services
      .AddKeyedTransient<IQueryResultAggregator, AvailabilityAggregator>(nameof(AvailabilityAggregator))
      .AddKeyedTransient<IQueryResultAggregator, GenericDistributionAggregator>(nameof(GenericDistributionAggregator))
      .AddKeyedTransient<IQueryResultAggregator, DemographicsDistributionAggregator>(
        nameof(DemographicsDistributionAggregator));

    // Beacon
    b.Services
      .Configure<BaseBeaconOptions>()
      .Configure<RelayBeaconOptions>()
      .AddTransient<IFilteringTermsService, FilteringTermsService>()
      .AddTransient<IndividualsQueryService>();

    // Hosted Services
    var isUpstreamTaskApiEnabled = b.Configuration.IsEnabled<TaskApiPollingOptions>();
    if (isUpstreamTaskApiEnabled)
      b.Services
        .AddHostedService<BackgroundUpstreamTaskPoller>()
        .AddScoped<ScopedTaskHandler>();

    b.Services.AddHostedService<TaskCompletionHostedService>();

    // Monitoring
    b.Services.Configure<MonitoringOptions>();
    b.Services.AddHealthChecks()
      .AddDbContextCheck<ApplicationDbContext>();

    return b;
  }
}
