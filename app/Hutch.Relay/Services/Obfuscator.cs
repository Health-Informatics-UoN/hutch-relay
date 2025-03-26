using Hutch.Relay.Config;
using Hutch.Relay.Services.Contracts;
using Microsoft.Extensions.Options;

namespace Hutch.Relay.Services;

public class Obfuscator(IOptions<ObfuscationOptions> obfuscationOptions) : IObfuscator
{
  public ObfuscationOptions GetObfuscationOptions()
    => obfuscationOptions.Value;
}
