using System;

namespace StorageObject.Interfaces
{
    public interface IObjectStorageInfo
    {
        string OriginalPath { get; set; }

        string Path { get; set; }

        string FileName { get; set; }

        string ETag { get; set; }

        DateTimeOffset CreatedDate { get; }

        string Sender { get; set; }
    }
}
