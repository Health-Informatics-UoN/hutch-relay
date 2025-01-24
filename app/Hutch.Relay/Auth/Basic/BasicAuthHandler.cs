using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Hutch.Relay.Data;
using Hutch.Relay.Data.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;


namespace Hutch.Relay.Auth.Basic;

internal class BasicAuthHandler : AuthenticationHandler<BasicAuthSchemeOptions>
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<RelayUser> _userManager;

        [Obsolete("Obsolete")]
        public BasicAuthHandler(
            IOptionsMonitor<BasicAuthSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            ApplicationDbContext db, UserManager<RelayUser> userManager)
            : base(options, logger, encoder, clock)
        {
          _db = db;
          _userManager = userManager;
        }

        private (string username, string password) ParseBasicAuthHeader(string authorizationHeader)
        {
            AuthenticationHeaderValue.TryParse(authorizationHeader, out var header);

            if (string.IsNullOrWhiteSpace(header?.Parameter))
            {
                const string noCredentialsMessage = "No Credentials.";
                Logger.LogError(noCredentialsMessage);
                throw new BasicAuthParsingException(noCredentialsMessage);
            }

            List<string> credentialsParts;

            try
            {
                // decode the header parameter
                credentialsParts = Encoding.UTF8.GetString(
                        Convert.FromBase64String(header.Parameter))
                    .Split(":", 2) // split at the first colon only
                    .ToList();
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Failed to decode credentials: {header.Parameter}.");

                throw new BasicAuthParsingException(
                    $"Failed to decode credentials: {header.Parameter}.",
                    e);
            }

            if (credentialsParts.Count < 2)
            {
                const string invalidCredentials = "Invalid credentials: missing delimiter.";
                Logger.LogError(invalidCredentials);
                throw new BasicAuthParsingException(invalidCredentials);
            }

            return (credentialsParts[0], credentialsParts[1]);
        }

        private async Task<ClaimsPrincipal?> Authenticate(string clientId, string clientSecret)
        {
          // Get user
          var user = await _userManager.FindByNameAsync(clientId);
          if (user == null)
          {
            Logger.LogWarning($"User not found: {clientId}");
            return null;
          }

          // Validate password
          var isPasswordValid = await _userManager.CheckPasswordAsync(user, clientSecret);
          if (!isPasswordValid)
          {
            Logger.LogWarning($"Invalid password for client: {clientId}");
            return null;
          }

          // Create the Identity and Principal
          var identity = new ClaimsIdentity([], Scheme.Name);
          return new ClaimsPrincipal(identity);
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey("Authorization"))
                return AuthenticateResult.Fail("Missing Authorization Header");
            
            // Get collectionId from the route
            if (!Request.RouteValues.TryGetValue("collectionId", out var routeCollectionId) || routeCollectionId == null)
              return AuthenticateResult.Fail("Missing collectionId in route.");

            try
            {
                var (clientId, clientSecret) = ParseBasicAuthHeader(Request.Headers.Authorization!);
                
                // Check if the clientID matches the collectionId
                if (routeCollectionId.ToString() != clientId)
                {
                  Logger.LogWarning($"collectionId \'{routeCollectionId}\' does not match clientId \'{clientId}\'.");
                  return AuthenticateResult.Fail("Collection ID does not match client credentials.");
                }

                var claimsPrincipal = await Authenticate(clientId, clientSecret);

                if (claimsPrincipal is not null)
                {
                    Logger.LogInformation($"Credentials validated for Client: {clientId}");

                    return AuthenticateResult.Success(new AuthenticationTicket(
                        claimsPrincipal,
                        Scheme.Name
                    ));
                }
                else
                {
                    Logger.LogInformation($"Credentials failed validation for Client: {clientId}");
                    return AuthenticateResult.Fail("Invalid credentials.");
                }

            }
            catch (BasicAuthParsingException e)
            {
                return AuthenticateResult.Fail(e.Message);
            }
        }

        protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.Headers["WWW-Authenticate"] = $"{Scheme.Name} realm=\"{Options.Realm}\", charset=\"UTF-8\"";
            await base.HandleChallengeAsync(properties);
        }
    }
