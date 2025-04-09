using System.Text.Json;
using Hutch.Rackit.TaskApi.Models;
using Hutch.Relay.Models;

namespace Hutch.Relay.Services.JobResultAggregators;

public class AvailabilityAggregator(IObfuscator obfuscator) : IQueryResultAggregator
{
  public QueryResult Process(List<RelaySubTaskModel> subTasks)
  {
    // TODO: Availability Results *CAN* contain Results Files too
    // If (when) Relay will support this, those files will need to be aggregated
    // Similar to e.g. Demographics Distribution
    
    var aggregateCount = 0;

    // Aggregate across all valid subtasks
    foreach (var subTask in subTasks)
    {
      if (subTask.Result is null) continue;

      var result = JsonSerializer.Deserialize<JobResult>(subTask.Result);
      // Don't crash if we can't parse the results;
      // Today we just pretend like that downstream client didn't respond and skip it
      // TODO: review this behaviour
      if (result is null) continue;

      aggregateCount += result.Results.Count;
    }

    // TODO: Review behaviour if no subtasks were valid:
    // should we report failure Upstream instead of a 0 count?

    // Apply obfuscation as configured by the call site
    var finalCount = obfuscator.Obfuscate(aggregateCount);

    return new()
    {
      Count = finalCount,
    };
  }
}
