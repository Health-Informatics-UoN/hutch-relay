using Hutch.Relay.Config;
using Hutch.Relay.Config.Beacon;
using Hutch.Relay.Constants;
using Hutch.Relay.Models.Beacon;
using Hutch.Relay.Services;
using Hutch.Relay.Services.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement.Mvc;

namespace Hutch.Relay.Controllers.Beacon;

[FeatureGate(Features.Beacon)]
[ApiController]
[Route($"{BeaconApiConstants.RoutePrefix}/[controller]")]
public class IndividualsController(
  IndividualsQueryService individuals,
  IFilteringTermsService filteringTerms,
  IOptions<RelayBeaconOptions> options) : ControllerBase
{
  /// <summary>
  /// Return a Summary of Individuals matching the provided filter criteria.
  /// </summary>
  /// <param name="filters">Filtering terms to match against</param>
  /// <param name="skip">Results records to skip; ignored by Relay due to lack of granularity support.</param>
  /// <param name="limit">Results records to return; ignored by Relay due to lack of granularity support.</param>
  /// <param name="requestedSchema">Schema for returned results records; ignored by Relay due to lack of granularity support.</param>
  /// <returns></returns>
  [HttpGet]
  public async Task<EntryTypeResponse> GetIndividuals(
    [FromQuery] List<string> filters,
    [FromQuery] int skip = 0,
    [FromQuery] int limit = 0,
    [FromQuery] string requestedSchema = "")
  {
    var granularity = options.Value.SecurityAttributes.DefaultGranularity;

    // prep response meta based on config and request
    Meta meta = new()
    {
      ReturnedGranularity = granularity.ToString()
    };

    var matchedTerms = await filteringTerms.Find(filters); // TODO: if none match, check if we should queue a terms update?

    // If not all terms are found, you can't even send a valid Availability request, since VarCat is now required (?)
    // but we can't populate VarCat without the terms cache (or a complete OMOP database?)

    if (matchedTerms.Count != filters.Count) // Beacon terms rules are always AND, so any missing term means false
      return new()
      {
        Meta = meta,
        ResponseSummary = individuals.GetEmptySummary()
      };

    // Boolean matches don't require a downstream query as we don't need the exact count
    if (granularity == Granularity.boolean)
      return new()
      {
        Meta = meta,
        ResponseSummary = individuals.GetResultsSummary(1) // we know it's true; the count is irrelevant
      };

    // try and queue downstream for the query 
    var queueName = await individuals.EnqueueDownstream(matchedTerms);
    if (queueName is null)
      return new()
      {
        Meta = meta,
        ResponseSummary = individuals.GetEmptySummary()
      };

    var results = await individuals.AwaitResults(queueName);

    return new()
    {
      Meta = meta,
      ResponseSummary = results
    };
  }
}
