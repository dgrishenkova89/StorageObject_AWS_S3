using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.Runtime.Internal;
using Amazon.Runtime.Internal.Auth;
using Amazon.Runtime.Internal.Util;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StorageObject.Entities;
using StorageObject.Interfaces;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace StorageObject
{
    public class ObjectStorageService : IObjectStorageService
    {
        private const string SECRET_KEY = "test";

        private readonly AmazonS3Client _s3Client;
        private readonly ILogger _logger;

        public ObjectStorageSettings Settings { get; }

        public ObjectStorageService(IOptions<ObjectStorageSettings> options, ILogger logger) : this(options.Value, logger)
        {
        }

        public ObjectStorageService(ObjectStorageSettings settings, ILogger logger)
        {
            Settings = settings;
            var s3Configuration = new AmazonS3Config
            {
                ServiceURL = Settings.Url,
                AuthenticationRegion = Settings.Region,
                SignatureMethod = SigningAlgorithm.HmacSHA256,
                AuthenticationServiceName = Settings.ServiceKey
            };
            _s3Client = new AmazonS3Client(Settings.UserId, SECRET_KEY, s3Configuration);
            _logger = logger;
        }

        public async Task<IObjectStorageInfo> UploadAsync(string fileName, byte[] data)
        {
            if (string.IsNullOrEmpty(fileName) || data.Length == 0)
            {
                return null;
            }

            try
            {
                var putObjectRequest = new PutObjectRequest
                {
                    BucketName = Settings.Bucket,
                    Key = fileName,
                    ContentType = "text/csv"
                };

                var sign = new AWS4Signer(false);
                var signRequest = new DefaultRequest(putObjectRequest, Settings.ServiceKey)
                {
                    Endpoint = new Uri($"{Settings.Url}/{Settings.Bucket}/{fileName}"),
                    HttpMethod = "PUT",
                    CanonicalResource = $"{Settings.Url}/{Settings.Bucket}"
                };
                await using var newStream = new MemoryStream();
                newStream.Write(data, 0, data.Length);
                signRequest.ContentStream = newStream;
                putObjectRequest.InputStream = newStream;

                var signResult = sign.SignRequest(signRequest, _s3Client.Config, new RequestMetrics(), Settings.UserId, SECRET_KEY);
                if (signResult != null)
                {
                    putObjectRequest.Headers["X-Amz-Algorithm"] = Settings.Algorithm;
                    putObjectRequest.Headers["X-Amz-Authorization"] = signResult.ForAuthorizationHeader;
                    putObjectRequest.Headers["X-Amz-SignedHeaders"] = signResult.SignedHeaders;
                    putObjectRequest.Headers["X-Amz-Date"] = signResult.ISO8601DateTime;
                    putObjectRequest.Headers["X-Amz-Credential"] = signResult.Scope;
                    putObjectRequest.Headers["X-Amz-Signature"] = signResult.Signature;
                }

                newStream.Flush();

                var response = await _s3Client.PutObjectAsync(putObjectRequest);

                string uploadedFileUrl = _s3Client.GetPreSignedURL(new GetPreSignedUrlRequest()
                {
                    BucketName = Settings.Bucket,
                    Key = fileName,
                    Expires = DateTime.Now.AddHours(3)
                });

                return new ObjectStorageInfo
                {
                    OriginalPath = $"{Settings.Url}/{Settings.Bucket}/{fileName}",
                    Path = uploadedFileUrl,
                    ETag = response.ETag,
                    FileName = fileName,
                    Sender = Settings.Sender
                };
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                if (amazonS3Exception.ErrorCode != null &&
                    (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId") ||
                     amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                {
                    _logger.LogError("Check the provided AWS Credentials.", amazonS3Exception);
                }
                else
                {
                    _logger.LogError($"Error occurred: {amazonS3Exception.Message}", amazonS3Exception);
                }
            }
            catch (Exception exception)
            {
                _logger.LogError($"Error occurred: {exception.Message}", exception);
            }

            return null;
        }

        public async Task<string> GetAsync(IObjectStorageInfo storageInfo)
        {
            string result = string.Empty;

            if (storageInfo == null)
            {
                return result;
            }

            try
            {
                var getObjectRequest = new GetObjectRequest()
                {
                    BucketName = Settings.Bucket,
                    Key = storageInfo.FileName,
                    EtagToMatch = storageInfo.ETag
                };

                SignRequest(getObjectRequest, "GET", storageInfo.FileName);

                var response = await _s3Client.GetObjectAsync(getObjectRequest);

                using var reader = new StreamReader(response.ResponseStream);
                result = await reader.ReadToEndAsync();
                reader.Close();
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                if (amazonS3Exception.ErrorCode != null &&
                    (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId") ||
                     amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                {
                    _logger.LogError("Check the provided AWS Credentials.", amazonS3Exception);
                }
                else
                {
                    _logger.LogError("Error occurred: ", amazonS3Exception);
                }
            }

            return result;
        }

        public async Task DeleteAsync(IObjectStorageInfo storageInfo)
        {
            try
            {
                var deleteObjectRequest = new DeleteObjectRequest
                {
                    BucketName = Settings.Bucket,
                    Key = storageInfo.FileName
                };

                SignRequest(deleteObjectRequest, "DELETE", storageInfo.FileName);

                await _s3Client.DeleteObjectAsync(deleteObjectRequest);
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                if (amazonS3Exception.ErrorCode != null &&
                    (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId") ||
                     amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                {
                    _logger.LogError("Check the provided AWS Credentials.", amazonS3Exception);
                }
                else
                {
                    _logger.LogError("Error occurred: ", amazonS3Exception);
                }
            }
        }

        private void SignRequest(AmazonWebServiceRequest request, string methodName, string fileName)
        {
            var sign = new AWS4Signer(false);
            var signRequest = new DefaultRequest(request, Settings.ServiceKey)
            {
                Endpoint = new Uri($"{Settings.Url}/{Settings.Bucket}/{fileName}"),
                HttpMethod = methodName,
                CanonicalResource = $"{Settings.Url}/{Settings.Bucket}"
            };

            sign.Sign(signRequest, _s3Client.Config, new RequestMetrics(), Settings.UserId, SECRET_KEY);
        }
    }
}
