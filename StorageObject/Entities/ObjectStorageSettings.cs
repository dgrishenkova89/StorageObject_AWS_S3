namespace StorageObject.Entities
{
    public class ObjectStorageSettings
    {
        public string Url { get; set; }
        public string Bucket { get; set; }
        public string UserId { get; set; }
        public string Region { get; set; }
        public string ServiceKey { get; set; }
        public string Algorithm { get; set; }
        public string Sender { get; set; }
        public string DownloadPath { get; set; }
    }
}
