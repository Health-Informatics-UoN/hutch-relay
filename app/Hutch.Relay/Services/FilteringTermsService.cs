using Hutch.Rackit.TaskApi;
using Hutch.Rackit.TaskApi.Models;
using Hutch.Relay.Config.Beacon;
using Hutch.Relay.Constants;
using Hutch.Relay.Data;
using Hutch.Relay.Data.Entities;
using Hutch.Relay.Services.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Hutch.Relay.Services;

public class FilteringTermsService(
  ILogger<FilteringTermsService> logger,
  IOptions<RelayBeaconOptions> beaconOptions,
  ISubNodeService subNodes,
  IDownstreamTaskService downstreamTasks,
  ApplicationDbContext db)
{
  public async Task RequestUpdatedTerms()
  {
    if (!beaconOptions.Value.Enable)
    {
      logger.LogWarning("GA4GH Beacon Functionality is disabled; not requesting updated Filtering Terms.");
      return;
    }

    // Get up-to-date Sub Nodes list
    var subnodes = (await subNodes.List()).ToList();

    var task = new CollectionAnalysisJob()
    {
      Uuid = RelayBeaconTaskDetails.IdPrefix + Guid.NewGuid().ToString(),
      Analysis = AnalysisType.Distribution,
      Code = DistributionCode.Generic,
      Collection = RelayBeaconTaskDetails.Collection,
      Owner = RelayBeaconTaskDetails.Owner
    };

    await downstreamTasks.Enqueue(task, subnodes);
  }

  public async Task CacheUpdatedTerms(JobResult finalResult)
  {
    // Try and get Generic Code Distribution ResultFile
    List<GenericDistributionRecord> distributionData = [];
    foreach (var file in finalResult.Results.Files)
    {
      // currently explicitly only process the first code distribution file
      // TODO: is it possible for more than one to be present and should we combine them?!
      if (distributionData.Count > 0) continue;

      if (file.FileName == ResultFileName.CodeDistribution)
      {
        var rawFileData = file.DecodeData();

        // Check we have more than just the header row; CsvHelper won't parse it if there's no actual data
        // This could happen if the QueryResult.Count was a lie ;) or just if the file was populated weirdly
        if (rawFileData.Split("\n").Length < 2) continue;

        // If we actually have data, go ahead and parse
        distributionData = ResultFileHelpers.ParseFileData<GenericDistributionRecord>(rawFileData);
      }
    }

    if (distributionData is [])
    {
      logger.LogWarning(
        "A FilteringTerms update was attempted from a downstream result that contains no Code Distribution Results. The update will not proceed.");
      return;
    }

    var filteringTerms = Map(distributionData);

    await using var transaction = await db.Database.BeginTransactionAsync();

    await db.FilteringTerms.ExecuteDeleteAsync();

    db.AddRange(filteringTerms);
    await db.SaveChangesAsync();

    await transaction.CommitAsync();
  }

  internal static List<FilteringTerm> Map(List<GenericDistributionRecord> records)
  {
    return [.. records.Select(Map)];
  }

  internal static FilteringTerm Map(GenericDistributionRecord record)
  {
    return new()
    {
      Term = record.Code,
      SourceCategory = record.Category,
      VarCat = CodeCategory.VarCatMap.GetValueOrDefault(record.Category),

      // Prefer OMOP Description if provided
      Description = string.IsNullOrWhiteSpace(record.OmopDescription)
        ? record.Description
        : record.OmopDescription
    };
  }
}
