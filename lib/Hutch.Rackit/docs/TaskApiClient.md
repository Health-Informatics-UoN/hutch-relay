# Task Api Client

RACKit currently provides a limited client for interacting with the RQuest Task API. In future further API surface of RQuest may also be covered by this library.

## Configuration

The Task API Client requires some information about the instance of RQuest it is connecting to:

- BaseUrl - the url of RQuest, that API endpoints can be appended to
- CollectionId - a default Collection ID
- Username - a valid API user for the target instance
- Password - the correct password for the above user

Client methods can be provided these details directly, or the service can be configured with default values, and they can be overridden if desirable when calling individual methods:

```csharp
// client is configured when constructed, optionally from DI
// but client defaults can be set later too:
client.Options = new ApiClientOptions {
  BaseUrl: "https://api.example.com"
  // ...
};

// Specify all details
await client.FetchNextJobAsync<AvailabilityJob>(
  "https://override.example.com",
  "RQ-Collection-123",
  "user1",
  "password1");

// Use all defaults as specified when constructing the client / via DI
await client.FetchNextJobAsync<AvailabilityJob>();

// Override some details - any omitted will fall back to the defaults
await client.FetchNextJobAsync<AvailabilityJob>(new () {
  CollectionId = "RQ-Collection-XYZ"
});
```

## `FetchNextJobAsync()`

Fetch Next Job interacts with the Task API's `/nextjob` endpoint, to retrieve the next job of a certain type for a given collection, if there is one.

The RACKit TaskApiClient currently supports the following job types:
- Availability
- CollectionAnalysis
  - Distribution

To specify the job type you want to fetch, specify the .NET Type for the query response model:

```csharp
await FetchQueryAsync<AvailabilityJob>();
await FetchQueryAsync<CollectionAnalysisJob>();
```

`FetchNextJob` returns null if there are no jobs in the queue.

`FetchNextJobAsync()` only requires the `ApiClientOptions` arguments.

## `SubmitResultAsync()`

Submit Result interacts with the Task API's `/result` endpoint, to submit the result for a job from a given collection.

SubmitResult will throw an exception if the request was not accepted by the remote Task API.

`SubmitResultAsync()` requires a `jobId` and a `result` payload, and also accepts the `ApiClientOptions` arguments.

Task API Job Result payloads are consistent regardless of job type, however there are three approaches to results submission:

- Without results files
- With inline results files
- With referenced results files

### Without results files

> [!NOTE]
> This is typical for Availability Jobs.

Without results files, usage is quite straightforward:

```csharp
// Collection Id and Job Id can be retrieved from the job per the response to `FetchQueryAsync()`
await SubmitResultAsync(
  jobId: "a030666b-2aed-4657-a126-498355ce89c4",
  result: new Result() {
    Uuid = "a030666b-2aed-4657-a126-498355ce89c4",
    CollectionId = "RQ-Collection-XYZ",
    Status = "OK",
    Message = "Results",
    Results = new()
    {
      Count = 123,
      DatasetCount = 1,
      Files = [] // When results files are not used, this should be an empty array
    }
});
```

### With inline results files

> [!NOTE]
> This is typical for Distribution Jobs, or other jobs where the results files are not too large.

With inline results files, usage is very similar to without, except that the files array includes objects describing the inline files, with file data included as a base 64 encoded string:

```csharp
// Collection Id and Job Id can be retrieved from the job per the response to `FetchQueryAsync()`
await SubmitResultAsync(
  jobId: "a030666b-2aed-4657-a126-498355ce89c4",
  result: new Result() {
    Uuid = "a030666b-2aed-4657-a126-498355ce89c4",
    CollectionId = "RQ-Collection-XYZ",
    Status = "OK",
    Message = "Results",
    Results = new()
    {
      Count = 123, // e.g. row count of TSV file data
      DatasetCount = 1,
      Files = [
        new ResultFile
        {
          FileDescription = "code.distribution analysis results",
        }
        .WithData( // encodes the data and sets FileData and FileSize properties for us
          """
          BIOBANK	CODE	COUNT	DESCRIPTION	MIN	Q1	MEDIAN	MEAN	Q3	MAX	ALTERNATIVES	DATASET	OMOP	OMOP_DESCR	CATEGORY
          <collection id>	OMOP:443614	123	nan	nan	nan	nan	nan	nan	nan	nan	nan	443614	Chronic kidney disease stage 1	Condition
          """)]
    }
});
```

Note that here we use the `WithData()` extension, which will take a plain string of tab separated data, or existing binary data, and do the base64 encoding for us. It is also possible to prepare the file data manually and directly assign the `FileData` and `FileSize` properties of the `ResultFile` object.

### With referenced results files

In this scenario, the Results File endpoint is first used to upload any file data, in the same `ResultFile` object format with base 64 encoding as when using inline files.

Once uploaded, the `/result` endpoint can then be used without including the file data.

Guidance here is yet to be finalised, as RACKit doesn't yet implement the separate Results File endpoint.
