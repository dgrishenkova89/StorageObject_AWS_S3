using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using StorageObject;
using StorageObject.Entities;
using StorageObject.Interfaces;

namespace StorageObjectTests
{
    public class ObjectStorageServiceTests
    {
        private ObjectStorageService _objectStorageSignature;
        private ObjectStorageSettings _settings;
        private string _etag;

        [SetUp]
        public void Setup()
        {
            _settings = new ObjectStorageSettings
            {
                Url = "https://storage.yandexcloud.net",
                Bucket = "test",
                UserId = "test",
                Algorithm = "AWS4-HMAC-SHA256",
                Region = "ru-central1",
                ServiceKey = "s3",
                Sender = "test@test.ru"
            };

            _objectStorageSignature = new ObjectStorageService(_settings, NullLogger.Instance);
        }

        [Test]
        [Order(1)]
        public void Should_send_file_to_server()
        {
            IObjectStorageInfo objectInfo = null;
            Assert.DoesNotThrow(() =>
                objectInfo = _objectStorageSignature.UploadAsync("test.csv", Encoding.UTF8.GetBytes(GetRegistry()))
                    .GetAwaiter()
                    .GetResult());

            Assert.That(objectInfo, Is.Not.Null);
            Assert.That(objectInfo.ETag, Is.Not.Null.And.Not.Empty);
            Assert.That(objectInfo.FileName, Is.Not.Null.And.Not.Empty);
            Assert.That(objectInfo.Path, Is.Not.Null.And.Not.Empty);

            _etag = objectInfo.ETag;
        }

        [Test]
        [Order(2)]
        public void Should_get_file_from_server()
        {
            var objectInfo = new ObjectStorageInfo
            {
                FileName = "test.csv",
                Path = $"{_settings.Url}/{_settings.Bucket}/test.csv",
                ETag = _etag
            };
            string file = string.Empty;
            Assert.DoesNotThrow(() =>
                file = _objectStorageSignature.GetAsync(objectInfo)
                    .GetAwaiter()
                    .GetResult());

            Assert.That(file, Is.Not.Null.And.Not.Empty);
            Assert.AreEqual(file, GetRegistry());
        }

        [Test]
        [Order(3)]
        public void Should_delete_file_from_server()
        {
            var objectInfo = new ObjectStorageInfo
            {
                FileName = "test.csv",
                Path = $"{_settings.Url}/{_settings.Bucket}/test.csv",
                ETag = _etag
            };
            Assert.DoesNotThrow(() =>
                _objectStorageSignature.DeleteAsync(objectInfo)
                    .GetAwaiter()
                    .GetResult());
        }

        private string GetRegistry()
        {
            return @"1965;Пиксель;E240 – формальдегид (опасный консервант)!;\""красный, зелёный, битый\"";\""3000,00\""
            1965; Мышка; \""А правильней использовать \""Ёлочки\""; ; \""4900,00\""
            \""Н/д\""; Кнопка; Сочетания клавиш; \""MUST USE! Ctrl, Alt, Shift\""; \""4799,00\""";
        }
    }
}