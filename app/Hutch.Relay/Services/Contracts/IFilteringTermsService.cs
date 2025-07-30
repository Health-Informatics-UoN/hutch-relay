using Hutch.Rackit.TaskApi.Models;

namespace Hutch.Relay.Services.Contracts;

public interface IFilteringTermsService
{
  Task CacheUpdatedTerms(JobResult finalResult);
  Task RequestUpdatedTerms();
}
