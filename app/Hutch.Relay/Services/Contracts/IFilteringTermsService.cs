using Hutch.Rackit.TaskApi.Models;
using Hutch.Relay.Models;
using Hutch.Relay.Models.Beacon;

namespace Hutch.Relay.Services.Contracts;

public interface IFilteringTermsService
{
  Task<bool> Any();
  Task CacheUpdatedTerms(JobResult finalResult);
  Task RequestUpdatedTerms(bool force = false);

  Task<bool> IsFilteringTermsRequestInProgress();

  Task<List<FilteringTerm>> List(int skip = 0, int limit = 10);

  Task<List<CachedFilteringTerm>> Find(List<string> termIds);
}
