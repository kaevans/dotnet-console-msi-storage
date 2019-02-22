using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dotnet_console_msi_storage
{
    class Program
    {
        private static readonly AzureServiceTokenProvider _azureServiceTokenProvider;

        static Program()
        {
            _azureServiceTokenProvider = new AzureServiceTokenProvider();
        }
        static void Main(string[] args)
        {
            if(args.Length == 0)
            {
                Console.WriteLine("Provide switch for create, delete, or list for blobs. Use switch for container to create the container named demo.");
            }
            else
            {
                switch(args[0])
                {
                    case "container":
                        CreateContainerIfNotExistsAsync().GetAwaiter().GetResult();
                        break;
                    case "create":
                        CreateBlobAsync().GetAwaiter().GetResult();
                        break;
                    case "delete":
                        DeleteAllBlobsAsync().GetAwaiter().GetResult();
                        break;
                    case "list":
                        ListBlobsAsync().GetAwaiter().GetResult();
                        break;
                    default:
                        Console.WriteLine("Provide switch for create, delete, or list");
                        break;
                }
            }
                        
        }
        private static async Task<CloudBlobContainer> GetContainerAsync()
        {
            var accessToken = await _azureServiceTokenProvider.GetAccessTokenAsync("https://storage.azure.com/");
            var tokenCredential = new TokenCredential(accessToken);
            StorageCredentials storageCredentials = new StorageCredentials(tokenCredential);

            string storageAccountUrl = ConfigurationManager.AppSettings["StorageAccountUrl"];
            var client = new CloudBlobClient(new Uri(storageAccountUrl), storageCredentials);

            var container = client.GetContainerReference("demo");
            return container;
        }

        public static async Task CreateContainerIfNotExistsAsync()
        {
            var container = await GetContainerAsync();
            await container.CreateIfNotExistsAsync();
        }

        public static async Task ListBlobsAsync()
        {

            var container = await GetContainerAsync();
            //await container.CreateIfNotExistsAsync();

            BlobContinuationToken blobContinuationToken = null;

            do
            {
                var results = await container.ListBlobsSegmentedAsync(null, blobContinuationToken);

                // Get the value of the continuation token returned by the listing call.
                blobContinuationToken = results.ContinuationToken;

                foreach (CloudBlockBlob item in results.Results)
                {
                    Console.WriteLine("{0} - {1}", item.Name, item.Uri);
                }

            } while (blobContinuationToken != null); // Loop while the continuation token is not null.


        }


        public static async Task CreateBlobAsync()
        {
            var container = await GetContainerAsync();

            var fileName = string.Format("{0}-{1}.txt", Guid.NewGuid().ToString(), System.DateTime.Now.Ticks);
            var blob = container.GetBlockBlobReference(fileName);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Hello world!");

            using (var stream = new MemoryStream(Encoding.Default.GetBytes(sb.ToString()), false))
            {
                blob.UploadFromStream(stream, null);
            }
        }

        public static async Task DeleteAllBlobsAsync()
        {
            var container = await GetContainerAsync();
            Parallel.ForEach(container.ListBlobs(), x => ((CloudBlob)x).Delete());
        }
    }
}
