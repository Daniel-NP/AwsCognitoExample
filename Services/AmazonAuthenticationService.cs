using Amazon;
using Amazon.CognitoIdentity;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Extensions.CognitoAuthentication;
using Amazon.Runtime;
using Amazon.SecurityToken;
using AwsCognitoExample.Helpers;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

namespace AwsCognitoExample.Services
{
    public class AuthenticationServiceOnBlazor
    {
        // sample data taken from https://github.com/aws-samples/aws-cognito-dot-net-desktop-app/blob/master/app.config

        private readonly RegionEndpoint awsRegionEndpoint = RegionEndpoint.USEast1;
        private readonly string poolId = "us-east-1_Bp9zDy6qR";
        private readonly string clientId = "60ddjpgc4jfo2fair3gi49ll1";
        private readonly string clientSecret = null;
        private readonly string identityPoolId = "us-east-1:146ea462-061b-42df-b8a5-a207d58555fb";        

        private readonly HttpClient httpClient;

        public CognitoResult CognitoResult { get; private set; }
        public bool IsLoggedIn => CognitoResult?.Type == CognitoResultType.Ok && CognitoResult?.TokenExpiresAt > DateTime.Now;


        public AuthenticationServiceOnBlazor(HttpClient client)
        {
            httpClient = client;
        }

        public async Task<bool> TryLoginAsync(string username, string password)
        {
            using (var client = new AmazonCognitoIdentityProviderClient(new AnonymousAWSCredentials(), CreateProviderConfig()))
            {
                var user = GetCognitoUser(client, username);
                var watch = Stopwatch.StartNew();

                try
                {

                    var authFlowResponse = await user.StartWithSrpAuthAsync(new InitiateSrpAuthRequest()
                    {
                        Password = password
                    }).ConfigureAwait(false);
                    if (authFlowResponse.ChallengeName == ChallengeNameType.NEW_PASSWORD_REQUIRED)
                    {
                        CognitoResult = new CognitoResult(CognitoResultType.PasswordChangeRequired, authFlowResponse, user);
                    }
                    else
                    {
                        Debug.WriteLine($"AWS Cognito > Get Credentials ({watch.Elapsed.TotalSeconds:N1}s elapsed)");
                        var credentials = await GetCredentialsWithCustomConfigAsync(user);
                        CognitoResult = new CognitoResult(CognitoResultType.Ok, authFlowResponse, user, credentials);

                        watch.Stop();
                        Debug.WriteLine($"AWS Cognito > Completed after {watch.Elapsed.TotalSeconds:N1}s");
                        return true;
                    }
                }
                catch (NotAuthorizedException exc1)
                {
                    CognitoResult = new CognitoResult(CognitoResultType.NotAuthorized)
                    {
                        Username = username,
                        Exception = exc1,
                        UserAgent = client.Config.UserAgent
                    };
                }
                catch (UserNotFoundException)
                {
                    CognitoResult = new CognitoResult(CognitoResultType.UserNotFound);
                }
                catch (UserNotConfirmedException)
                {
                    CognitoResult = new CognitoResult(CognitoResultType.NotConfirmed);
                }
                catch (OperationCanceledException exc4)
                {
                    CognitoResult = new CognitoResult(CognitoResultType.Timeout)
                    {
                        Username = username,
                        Exception = exc4,
                        UserAgent = client.Config.UserAgent
                    };
                }
                catch (Exception exc5)
                {
                    if (exc5.Message.ToLower().Contains("no such host is known"))
                    {
                        CognitoResult = new CognitoResult(CognitoResultType.Offline);
                    }
                    else
                    {
                        CognitoResult = new CognitoResult(CognitoResultType.Unknown);
                    }
                    CognitoResult.Username = username;
                    CognitoResult.Exception = exc5;
                    CognitoResult.UserAgent = client.Config.UserAgent;
                }
                watch.Stop();
                Debug.WriteLine($"AWS Cognito > Failed after {watch.Elapsed.TotalSeconds:N1}s");
            }
            return false;
        }

        private async Task<ImmutableCredentials> GetCredentialsWithCustomConfigAsync(CognitoUser user)
        {
            if (user.SessionTokens == null || !user.SessionTokens.IsValid())
            {
                throw new NotAuthorizedException("User is not authenticated.");
            }

            string poolRegion = user.UserPool.PoolID.Substring(0, user.UserPool.PoolID.IndexOf("_"));
            string providerName = "cognito-idp." + poolRegion + ".amazonaws.com/" + user.UserPool.PoolID;

            using (var awsCredentials = new CognitoAWSCredentials(null, identityPoolId, null, null,
                new AmazonCognitoIdentityClient(new AnonymousAWSCredentials(), CreateIdentityConfig()),
                new AmazonSecurityTokenServiceClient(new AnonymousAWSCredentials(), CreateSecurityTokenServiceConfig())))
            {
                awsCredentials.Clear();
                awsCredentials.AddLogin(providerName, user.SessionTokens.IdToken);

                var credentials = await awsCredentials.GetCredentialsAsync();
                return credentials;
            }
        }

        private CognitoUser GetCognitoUser(AmazonCognitoIdentityProviderClient client, string username)
        {
            var userPool = new CognitoUserPool(poolId, clientId, client, clientSecret);
            return new CognitoUser(username, clientId, userPool, client, clientSecret);
        }

        private AmazonCognitoIdentityProviderConfig CreateProviderConfig()
        {
            var config = new AmazonCognitoIdentityProviderConfig
            {
                RegionEndpoint = awsRegionEndpoint,
                HttpClientFactory = new BlazorHttpClientFactory(httpClient)
            };
            return config;
        }

        private AmazonCognitoIdentityConfig CreateIdentityConfig()
        {
            var config = new AmazonCognitoIdentityConfig
            {
                RegionEndpoint = awsRegionEndpoint,
                HttpClientFactory = new BlazorHttpClientFactory(httpClient)
            };
            return config;
        }

        private AmazonSecurityTokenServiceConfig CreateSecurityTokenServiceConfig()
        {
            var config = new AmazonSecurityTokenServiceConfig
            {
                RegionEndpoint = awsRegionEndpoint,
                HttpClientFactory = new BlazorHttpClientFactory(httpClient)
            };
            return config;
        }
    }
}
