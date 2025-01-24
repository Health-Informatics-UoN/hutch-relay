using Microsoft.AspNetCore.Identity;

namespace Hutch.Relay.Constants;

public static class DefaultIdentityOptions
{
  public static readonly Action<IdentityOptions> Configure = options =>
  {
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
  };
}
