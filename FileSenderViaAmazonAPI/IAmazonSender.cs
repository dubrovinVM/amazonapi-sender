using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace FileSenderViaAmazonAPI
{
    public interface IAmazonSender
    {
        string bucketName { get; set; }
        string keyName { get; set; }
        string filePath { get; set; }
        void ConfigureAppSettingFile();
        void SetFieldsFromAppConfig();
        Task UploadFileAsync();
        ILogger<IAmazonSender> logger { get; set; }
    }
}
