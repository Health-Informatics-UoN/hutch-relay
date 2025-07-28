using Hutch.Rackit.TaskApi.Models;
using Hutch.Relay.Models;

namespace Hutch.Relay.Services.Contracts;

public interface IDownstreamTaskService
{
  Task Enqueue<T>(T task, List<SubNodeModel> targets) where T : TaskApiBaseResponse;
}
