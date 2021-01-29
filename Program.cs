using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace AuthenticationRecordTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // 1. Create TokenCredential object with TokenCache set
            var credentialOne = new InteractiveBrowserCredential(
                new InteractiveBrowserCredentialOptions
                {
                    TokenCache = new PersistentTokenCache()
                });

            // 2. Prompt user to authenticate
            AuthenticationRecord authRecordWrite = await credentialOne.AuthenticateAsync();

            // 3. Save AuthenticationRecord to disk
            using var authRecordStreamWrite = new FileStream("AuthRecord.json", FileMode.Create, FileAccess.Write);
            await authRecordWrite.SerializeAsync(authRecordStreamWrite);
            await authRecordStreamWrite.FlushAsync();

            // A future user session where we want to silent auth with TokenCache and AuthenticationRecord

            // 4. Read the AutheticationRecord from disk
            using var authRecordStreamRead = new FileStream("AuthRecord.json", FileMode.Open, FileAccess.Read);
            AuthenticationRecord authRecordRead = await AuthenticationRecord.DeserializeAsync(authRecordStreamRead);

            // 5. Create TokenCredential object with TokenCache and use persisted AuthenticationRecord
            var credentialTwo = new InteractiveBrowserCredential(
                new InteractiveBrowserCredentialOptions
                {
                    TokenCache = new PersistentTokenCache(),
                    AuthenticationRecord = authRecordRead
                });

            // 6. Use the new TokenCredential object. User will not be prompted to re-authenticate if token has expired.
            var client = new SecretClient(new Uri("https://memealyzerdev9kv.vault.azure.net/"), credentialTwo);
            var secret = await client.GetSecretAsync("CosmosKey");

            Console.WriteLine(secret.Value.Value);
        }
    }
}