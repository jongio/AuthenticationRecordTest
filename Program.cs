using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace AuthenticationRecordTest
{
    class Program
    {
        static async Task Main(string[] args)
        {

            // var cred = new DefaultAzureCredential();
            // var token = cred.GetTokenAsync(
            //     new TokenRequestContext(scopes: new string[] { "your scope here" }) { }
            // );



            // 1. Create TokenCredential object with TokenCachePersistenceOptions set
            var credentialOne = new InteractiveBrowserCredential(
                new InteractiveBrowserCredentialOptions
                {
                    TokenCachePersistenceOptions = new TokenCachePersistenceOptions()
                    {
                        Name = "AuthenticationRecord.cache"
                    }
                });

            // 2. Prompt user to authenticate
            AuthenticationRecord authRecordWrite = await credentialOne.AuthenticateAsync();

            // 3. Save AuthenticationRecord to disk
            using (var authRecordStreamWrite = new FileStream("AuthRecord.json", FileMode.Create, FileAccess.Write))
            {
                await authRecordWrite.SerializeAsync(authRecordStreamWrite);
            }

            // A future user session where we want to silent auth with TokenCache and AuthenticationRecord

            // 4. Read the AuthenticationRecord from disk
            AuthenticationRecord authRecordRead;
            using (var authRecordStreamRead = new FileStream("AuthRecord.json", FileMode.Open, FileAccess.Read))
            {
                authRecordRead = await AuthenticationRecord.DeserializeAsync(authRecordStreamRead);
            }

            // 5. Create TokenCredential object with TokenCache and use persisted AuthenticationRecord
            var credentialTwo = new InteractiveBrowserCredential(
                new InteractiveBrowserCredentialOptions
                {
                    TokenCachePersistenceOptions = new TokenCachePersistenceOptions()
                    {
                        Name = "AuthenticationRecord.cache"
                    },
                    AuthenticationRecord = authRecordRead
                });



            // 5.1 Same as above but with DisableAutomaticAuthentication set to true
            // var credentialTwo = new InteractiveBrowserCredential(
            //     new InteractiveBrowserCredentialOptions
            //     {
            //         TokenCachePersistenceOptions = new TokenCachePersistenceOptions()
            //         {
            //             Name = "AuthenticationRecord.cache"
            //         },
            //         AuthenticationRecord = authRecordRead,
            //         DisableAutomaticAuthentication = true
            //     });

            // 6. Use the new TokenCredential object. User will not be prompted to re-authenticate if token has expired.
            var client = new SecretClient(new Uri("https://memealyzerdevkv.vault.azure.net/"), credentialTwo);

            try
            {
                var secret = await client.GetSecretAsync("CosmosKey");
                Console.WriteLine(secret.Value.Value);

            }
            catch (AuthenticationRequiredException ex)
            {
                Console.WriteLine("You set InteractiveBrowserCredentialOptions.DisableAutomaticAuthentication to true and the user token has expired or has been revoked.");
                Console.WriteLine(ex.ToString());
            }
        }
    }
}