namespace Hutch.Rackit.Tests.ResultFileExtensionsTests;

public static class Base64TestData
{
  public const string CodeDistributionRaw = """
                                    BIOBANK	CODE	COUNT	DESCRIPTION	MIN	Q1	MEDIAN	MEAN	Q3	MAX	ALTERNATIVES	DATASET	OMOP	OMOP_DESCR	CATEGORY
                                    collection_id	OMOP:38003564	40										38003564	Not Hispanic or Latino	Ethnicity
                                    collection_id	OMOP:38003563	60										38003563	Hispanic or Latino	Ethnicity
                                    collection_id	OMOP:8507	40										8507	MALE	Gender
                                    collection_id	OMOP:8532	60										8532	FEMALE	Gender
                                    """;
  public const string CodeDistributionBase64 = "QklPQkFOSwlDT0RFCUNPVU5UCURFU0NSSVBUSU9OCU1JTglRMQlNRURJQU4JTUVBTglRMwlNQVgJQUxURVJOQVRJVkVTCURBVEFTRVQJT01PUAlPTU9QX0RFU0NSCUNBVEVHT1JZCmNvbGxlY3Rpb25faWQJT01PUDozODAwMzU2NAk0MAkJCQkJCQkJCQkzODAwMzU2NAlOb3QgSGlzcGFuaWMgb3IgTGF0aW5vCUV0aG5pY2l0eQpjb2xsZWN0aW9uX2lkCU9NT1A6MzgwMDM1NjMJNjAJCQkJCQkJCQkJMzgwMDM1NjMJSGlzcGFuaWMgb3IgTGF0aW5vCUV0aG5pY2l0eQpjb2xsZWN0aW9uX2lkCU9NT1A6ODUwNwk0MAkJCQkJCQkJCQk4NTA3CU1BTEUJR2VuZGVyCmNvbGxlY3Rpb25faWQJT01PUDo4NTMyCTYwCQkJCQkJCQkJCTg1MzIJRkVNQUxFCUdlbmRlcg==";

}
