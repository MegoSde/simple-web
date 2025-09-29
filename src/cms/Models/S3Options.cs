namespace cms.Models;

public sealed class S3Options
{
    public string Endpoint { get; set; } = "";
    public string Region { get; set; } = "us-east-1";
    public bool UseSsl { get; set; } = false;
    public string AccessKey { get; set; } = "";
    public string SecretKey { get; set; } = "";
    public BucketsOptions Buckets { get; set; } = new();
    public sealed class BucketsOptions
    {
        public string Originals { get; set; } = "originals";
        public string Derived { get; set; } = "derived";
    }
}