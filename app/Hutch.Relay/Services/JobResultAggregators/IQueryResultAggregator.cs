using Hutch.Rackit.TaskApi.Models;
using Hutch.Relay.Config;
using Hutch.Relay.Models;

namespace Hutch.Relay.Services.JobResultAggregators;

public interface IQueryResultAggregator
{
  public QueryResult Process(List<RelaySubTaskModel> subTasks);
}
