using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FileSenderViaAmazonAPI
{
    public class AmazonSender : IAmazonSender
    {
        public string bucketName { get; set; }
        public string keyName { get; set; }
        public string filePath { get; set; }
        public RegionEndpoint bucketRegion;
        public IAmazonS3 s3Client;
        public IConfiguration config;
        public ILogger<IAmazonSender> logger { get; set; }
        private InitiateMultipartUploadResponse initResponse;

        public AmazonSender()
        {
            ConfigureAppSettingFile();
            SetFieldsFromAppConfig();
            s3Client = new AmazonS3Client(bucketRegion);
        }

        public async Task UploadFileAsync()
        {
            // Create list to store upload part responses.
            List<UploadPartResponse> uploadResponses = new List<UploadPartResponse>();

            // Setup information required to initiate the multipart upload.
            InitiateMultipartUploadRequest initiateRequest = new InitiateMultipartUploadRequest
            {
                BucketName = bucketName,
                Key = keyName
            };

            // Initiate the upload.
            try
            {
                initResponse = await s3Client.InitiateMultipartUploadAsync(initiateRequest);
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Initiating the upload failed: {exception.Message}");
                logger.LogError($"Initiating the upload failed: {exception.Message}");
                Console.ReadKey();
                return;
            }

            // Upload parts.
            long contentLength = new FileInfo(filePath).Length;
            long partSize = 5 * (long)Math.Pow(2, 20); // 5 MB

            try
            {
                Console.WriteLine("Uploading parts...");
                logger.LogInformation("Uploading parts...");
                long filePosition = 0;
                for (int i = 1; filePosition < contentLength; i++)
                {
                    UploadPartRequest uploadRequest = new UploadPartRequest
                    {
                        BucketName = bucketName,
                        Key = keyName,
                        UploadId = initResponse.UploadId,
                        PartNumber = i,
                        PartSize = partSize,
                        FilePosition = filePosition,
                        FilePath = filePath
                    };

                    // Track upload progress.
                    uploadRequest.StreamTransferProgress += new EventHandler<StreamTransferProgressArgs>(UploadPartProgressEventCallback);

                    // Upload a part and add the response to our list.
                    uploadResponses.Add(await s3Client.UploadPartAsync(uploadRequest));

                    filePosition += partSize;
                }

                // Setup to complete the upload.
                CompleteMultipartUploadRequest completeRequest = new CompleteMultipartUploadRequest
                {
                    BucketName = bucketName,
                    Key = keyName,
                    UploadId = initResponse.UploadId
                };
                completeRequest.AddPartETags(uploadResponses);
                logger.LogInformation($"Complete Request send");
                // Complete the upload.
                CompleteMultipartUploadResponse completeUploadResponse = await s3Client.CompleteMultipartUploadAsync(completeRequest);
                logger.LogInformation($"Complete Request ok");
            }
            catch (Exception exception)
            {
                Console.WriteLine($"An AmazonS3Exception was thrown: {exception.Message}");
                logger.LogError($"An AmazonS3Exception was thrown: {exception.Message}");

                // Abort the upload.
                AbortMultipartUploadRequest abortMPURequest = new AbortMultipartUploadRequest
                {
                    BucketName = bucketName,
                    Key = keyName,
                    UploadId = initResponse.UploadId
                };
                await s3Client.AbortMultipartUploadAsync(abortMPURequest);
                logger.LogError($"Multipart Upload Async Aborted!");
            }
        }

        public static void UploadPartProgressEventCallback(object sender, StreamTransferProgressArgs e)
        {
            // Process event. 
            Console.WriteLine("{0}/{1}", e.TransferredBytes, e.TotalBytes);
        }

        public void ConfigureAppSettingFile()
        {
            config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .Build();

            bucketRegion = RegionEndpoint.USWest2;
        }

        public void SetFieldsFromAppConfig()
        {
            bucketName = config["bucketName"];
            keyName = config["keyName"];
            filePath = config["filePath"];
        }
    }
}
