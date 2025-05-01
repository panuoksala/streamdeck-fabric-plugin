using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using StreamDeckMicrosoftFabric.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StreamDeckMicrosoftFabric.Services
{
    public class LoginService(ILogger _logger, FabricSettingsModel _model)
    {
        public DateTimeOffset? LoginExpires { get; set; }
        public string AccessToken { get; set; }

        public bool IsLoginValid()
        {
            // First time login
            if (LoginExpires is null)
            {
                return false;
            }

            // Expired, relogin
            if (DateTimeOffset.Now > LoginExpires)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Implemented from this guide:
        /// https://github.com/Azure-Samples/ms-identity-dotnet-desktop-tutorial/blob/master/1-Calling-MSGraph/1-1-AzureAD/README.md
        /// </summary>
        /// <returns></returns>
        public async Task Login()
        {
            string authority = $"https://login.microsoftonline.com/{_model.TenantId}/";
            string[] scopes = { "https://api.fabric.microsoft.com/.default" };

            if ((LoginMethod)_model.LoginMethod == LoginMethod.AppRegistration)
            {
                string ClientId = _model.ClientId;
                string ClientSecret = _model.Secret;                

                IConfidentialClientApplication confidentialClientApp = ConfidentialClientApplicationBuilder.Create(ClientId)
                    .WithClientSecret(ClientSecret)
                    .WithAuthority(new Uri(authority))
                    .Build();

                try
                {
                    AuthenticationResult result = await confidentialClientApp
                        .AcquireTokenForClient(scopes)
                        .ExecuteAsync();

                    if (!string.IsNullOrEmpty(result.AccessToken))
                    {
                        LoginExpires = result.ExpiresOn;
                        AccessToken = result.AccessToken;
                    }
                    else
                    {
                        throw new Exception("Login failed. Check application ID and secret.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to login.");
                }
            }
            else
            {
                var options = new InteractiveBrowserCredentialOptions
                {
                    AuthorityHost = new Uri(authority),
                    TenantId = _model.TenantId
                };

                var credential = new InteractiveBrowserCredential(options);
                try
                {
                    var tokenRequestContext = new TokenRequestContext(scopes);
                    var token = await credential.GetTokenAsync(tokenRequestContext);

                    if (!string.IsNullOrEmpty(token.Token))
                    {
                        LoginExpires = token.ExpiresOn;
                        AccessToken = token.Token;
                    }
                    else
                    {
                        throw new Exception("Login failed in interactive mode. Check user credentials.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to login with interactive mode.");
                }
            }
        }
    }
}
