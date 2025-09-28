namespace cms.Models;

public sealed class HealthOptions
{
    public MinioHealthOptions Minio { get; set; } = new();
    public sealed class MinioHealthOptions
    {
        public bool Enabled { get; set; } = true;
        public string Bucket { get; set; } = "originals";
    }
}