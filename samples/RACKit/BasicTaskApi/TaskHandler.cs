using System.Text.Json;
using Hutch.Rackit.TaskApi;
using Hutch.Rackit.TaskApi.Models;
using Microsoft.Extensions.Logging;

namespace BasicTaskApi;

public class TaskHandler(ILogger<TaskHandler> logger, TaskApiClient client)
{
  public int TaskDelayMs { get; set; } = 10000;
  
  public async Task HandleAvailabilityJob(AvailabilityJob job)
  {
    logger.LogInformation("Found Availability job: {Job}", JsonSerializer.Serialize(job));

    await Task.Delay(TaskDelayMs); // Wait while we "query". Nice for the GUI to show "sent to client" vs "job done"

    await client.SubmitResultAsync(job.Uuid, new()
    {
      Uuid = job.Uuid,
      CollectionId = job.Collection,
      Status = "OK",
      Message = "Results",
      Results = new()
      {
        Count = 123,
        DatasetCount = 1,
        Files = []
      }
    });

    logger.LogInformation("Response sent for Availability job: {JobId}", job.Uuid);
  }

  public async Task HandleCollectionAnalysisJob(CollectionAnalysisJob job)
  {
    logger.LogInformation("Found Collection Analysis job: {Job}", JsonSerializer.Serialize(job));

    await Task.Delay(TaskDelayMs); // Wait while we "query". Nice for the GUI to show "sent to client" vs "job done"

    var codeDistributionResult = new QueryResult
    {
      Count = 1,
      DatasetCount = 1,
      Files = [
        new ResultFile
          {
            FileDescription = "code.distribution analysis results",
          }
          .WithData( // encodes the data and sets FileData and FileSize properties for us
            $"""
            BIOBANK	CODE	COUNT	DESCRIPTION	MIN	Q1	MEDIAN	MEAN	Q3	MAX	ALTERNATIVES	DATASET	OMOP	OMOP_DESCR	CATEGORY
            {job.Collection}	OMOP:443614	123										443614	Chronic kidney disease stage 1	Condition
            """)
      ]
    };
    
    var demographicsDistributionResult = new QueryResult
    {
      Count = 1,
      DatasetCount = 1,
      Files = [
        new ResultFile
          {
            FileDescription = "demographics.distribution analysis results",
          }
          .WithData( // encodes the data and sets FileData and FileSize properties for us
            $"""
            BIOBANK	CODE	DESCRIPTION	COUNT	MIN	Q1	MEDIAN	MEAN	Q3	MAX	ALTERNATIVES	DATASET	OMOP	OMOP_DESCR	CATEGORY
            {job.Collection}	SEX	Sex	99							^MALE|44^FEMALE|55^	person			DEMOGRAPHICS
            """
            )
      ]
    };

    var unhandledResults = new QueryResult
    {
      Count = 0,
      DatasetCount = 0,
      Files = []
    };

    await client.SubmitResultAsync(job.Uuid, new()
    {
      Uuid = job.Uuid,
      CollectionId = job.Collection,
      Status = "OK",
      Message = "Results",
      Results = job.Analysis switch
      {
        AnalysisType.Distribution => job.Code switch
        {
          DistributionCode.Generic => codeDistributionResult,
          DistributionCode.Demographics => demographicsDistributionResult,
          _ => unhandledResults
        },
        _ => unhandledResults
      }
    });

    logger.LogInformation("Response sent for job: {JobId}", job.Uuid);
  }
}
