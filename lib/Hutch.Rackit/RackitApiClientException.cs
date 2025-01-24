namespace Hutch.Rackit;

public class RackitApiClientException(string? message, HttpResponseMessage? response = null) : Exception(message)
{
  public HttpResponseMessage? UpstreamApiResponse { get; set; } = response;
}
