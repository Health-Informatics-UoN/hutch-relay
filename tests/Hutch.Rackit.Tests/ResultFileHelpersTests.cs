using Hutch.Rackit.TaskApi.Models;

namespace Hutch.Rackit.Tests;

public class ResultFileHelpersTests
{
  [Fact]
  public void ParseFileData_QuotesInCellValue_ParsesCorrectly()
  {
    // Construct a TSV with quotes inside cell values
    var tsvData = """
    BIOBANK	CODE	COUNT	DESCRIPTION	MIN	Q1	MEDIAN	MEAN	Q3	MAX	ALTERNATIVES	DATASET	OMOP	OMOP_DESCR	CATEGORY
    collection_id	OMOP:38003564	40										38003564	Not Hispanic or Latino	Ethnicity
    collection_id	OMOP:38003563	60										38003563	Hispanic or Latino	Ethnicity
    collection_id	OMOP:8507	40										8507	MALE	Gender
    collection_id	OMOP:8532	60										8532	FEMALE	Gender
    collection_id	OMOP:35810807	20										35810807	Word associated with "tiny"	Observation
    """;

    List<GenericDistributionRecord> expected = [
    new()
    {
      Collection = "collection_id",
      Code = "OMOP:38003564",
      Count = 40,
      OmopCode = 38003564,
      OmopDescription = "Not Hispanic or Latino",
      Category = "Ethnicity"
    },
    new()
    {
      Collection = "collection_id",
      Code = "OMOP:38003563",
      Count = 60,
      OmopCode = 38003563,
      OmopDescription = "Hispanic or Latino",
      Category = "Ethnicity"
    },
    new()
    {
      Collection = "collection_id",
      Code = "OMOP:8507",
      Count = 40,
      OmopCode = 8507,
      OmopDescription = "MALE",
      Category = "Gender"
    },
    new()
    {
      Collection = "collection_id",
      Code = "OMOP:8532",
      Count = 60,
      OmopCode = 8532,
      OmopDescription = "FEMALE",
      Category = "Gender"
    },
    new()
    {
      Collection = "collection_id",
      Code = "OMOP:35810807",
      Count = 20,
      OmopCode = 35810807,
      OmopDescription = "Word associated with \"tiny\"",
      Category = "Observation"
    }
  ];

    var actual = ResultFileHelpers.ParseFileData<GenericDistributionRecord>(tsvData);

    Console.WriteLine();

    // Success if we don't throw to be honest 👍
    // But we also should check the results match the input
    Assert.Equivalent(expected, actual);
    Assert.Equal(expected.Last().OmopDescription, actual.Last().OmopDescription);
  }
}

