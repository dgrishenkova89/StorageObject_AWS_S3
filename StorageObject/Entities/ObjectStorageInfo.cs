using System;
using StorageObject.Interfaces;

namespace StorageObject.Entities
{
    public class ObjectStorageInfo : IObjectStorageInfo
    {
        public string OriginalPath { get; set; }
        public string Path { get; set; }
        public string FileName { get; set; }
        public string ETag { get; set; }
        public DateTimeOffset CreatedDate => DateTimeOffset.UtcNow;
        public string Sender { get; set; }
    }
}
