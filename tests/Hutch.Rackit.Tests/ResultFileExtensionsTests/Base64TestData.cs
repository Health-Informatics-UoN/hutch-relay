using Hutch.Rackit.TaskApi.Models;

namespace Hutch.Rackit.Tests.ResultFileExtensionsTests;

public static class Base64TestData
{
  // Matching Code Distribution data
  public static readonly List<GenericDistributionRecord> CodeDistributionList =
  [
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
    }
  ];

  public const string CodeDistributionRaw = """
                                            BIOBANK	CODE	COUNT	DESCRIPTION	MIN	Q1	MEDIAN	MEAN	Q3	MAX	ALTERNATIVES	DATASET	OMOP	OMOP_DESCR	CATEGORY
                                            collection_id	OMOP:38003564	40										38003564	Not Hispanic or Latino	Ethnicity
                                            collection_id	OMOP:38003563	60										38003563	Hispanic or Latino	Ethnicity
                                            collection_id	OMOP:8507	40										8507	MALE	Gender
                                            collection_id	OMOP:8532	60										8532	FEMALE	Gender
                                            """;

  public const string CodeDistributionBase64 =
    "QklPQkFOSwlDT0RFCUNPVU5UCURFU0NSSVBUSU9OCU1JTglRMQlNRURJQU4JTUVBTglRMwlNQVgJQUxURVJOQVRJVkVTCURBVEFTRVQJT01PUAlPTU9QX0RFU0NSCUNBVEVHT1JZCmNvbGxlY3Rpb25faWQJT01PUDozODAwMzU2NAk0MAkJCQkJCQkJCQkzODAwMzU2NAlOb3QgSGlzcGFuaWMgb3IgTGF0aW5vCUV0aG5pY2l0eQpjb2xsZWN0aW9uX2lkCU9NT1A6MzgwMDM1NjMJNjAJCQkJCQkJCQkJMzgwMDM1NjMJSGlzcGFuaWMgb3IgTGF0aW5vCUV0aG5pY2l0eQpjb2xsZWN0aW9uX2lkCU9NT1A6ODUwNwk0MAkJCQkJCQkJCQk4NTA3CU1BTEUJR2VuZGVyCmNvbGxlY3Rpb25faWQJT01PUDo4NTMyCTYwCQkJCQkJCQkJCTg1MzIJRkVNQUxFCUdlbmRlcg==";


  // Matching Demographics Distribution data
  public static readonly List<DemographicsDistributionRecord> DemographicsDistributionList =
  [
    new()
    {
      Collection = "collection_id",
      Code = "SEX",
      Description = "Sex",
      Count = 99,
      Alternatives = "^MALE|44^FEMALE|55^",
      Dataset = "person",
      Category = "DEMOGRAPHICS"
    }
  ];
  public const string DemographicsDistributionRaw = """
                                                    BIOBANK	CODE	DESCRIPTION	COUNT	MIN	Q1	MEDIAN	MEAN	Q3	MAX	ALTERNATIVES	DATASET	OMOP	OMOP_DESCR	CATEGORY
                                                    collection_id	SEX	Sex	99							^MALE|44^FEMALE|55^	person			DEMOGRAPHICS
                                                    """;

  public const string DemographicsDistributionBase64 = "QklPQkFOSwlDT0RFCURFU0NSSVBUSU9OCUNPVU5UCU1JTglRMQlNRURJQU4JTUVBTglRMwlNQVgJQUxURVJOQVRJVkVTCURBVEFTRVQJT01PUAlPTU9QX0RFU0NSCUNBVEVHT1JZCmNvbGxlY3Rpb25faWQJU0VYCVNleAk5OQkJCQkJCQleTUFMRXw0NF5GRU1BTEV8NTVeCXBlcnNvbgkJCURFTU9HUkFQSElDUw==";
}
