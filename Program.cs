using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace SamplePageBlobUpload
{
    class Program
    {
        // Path to the file you want to upload
        static readonly string file = @"";

        // Valid storage container name
        static readonly string containerName = "";

        // Valid storage blob name
        static readonly string blobName = "";

        // Connections string for the destination storage account
        static readonly string storageAccountConnectionString = "";


        static void Main(string[] args)
        {
            CloudPageBlob blob = null;
            try
            {
                var storageAccount = CloudStorageAccount.Parse(storageAccountConnectionString);
                var blobClient = storageAccount.CreateCloudBlobClient();
                var container = blobClient.GetContainerReference(containerName);
                container.CreateIfNotExists();
                blob = container.GetPageBlobReference(blobName);
            }
            catch (StorageException ex)
            {
                Console.WriteLine("Failed to get blob");
                if (ex.InnerException is WebException)
                {
                    var wex = ex.InnerException as WebException;
                    if (wex.Response is HttpWebResponse)
                    {
                        var response = wex.Response as HttpWebResponse;
                        Console.WriteLine(response.StatusDescription);
                    }
                }
                throw ex;
            }

            if (blob.Exists())
                throw new Exception("The target blob already exists");

            if (!File.Exists(file))
                throw new Exception("The file doesn't exist");

            UploadFile(blob, file).Wait();
        }

        static async Task UploadFile(CloudPageBlob blob, string path)
        {
            using (var fileStream = File.OpenRead(path))
            {
                var fileSize = fileStream.Length;
                Console.WriteLine("File size is {0} bytes", fileSize);

                var sizeToPad = 512 - (int)(fileSize % 512);
                Console.WriteLine("Need to pad with {0} bytes", sizeToPad);

                using (var blobStream = await blob.OpenWriteAsync(fileSize + sizeToPad))
                {
                    Console.WriteLine("Writing file bytes");
                    await fileStream.CopyToAsync(blobStream);

                    Console.WriteLine("Padding page blob");
                    await blobStream.WriteAsync(new byte[sizeToPad], 0, sizeToPad);
                }
            }
        }
    }
}
