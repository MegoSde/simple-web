using Amazon.S3;

namespace cms.Services;

public static class S3ClientFactory {
    public static IAmazonS3 Create(IConfiguration cfg) {
        var ep = cfg["Storage:S3:Endpoint"]!;
        var ak = cfg["Storage:S3:AccessKey"]!;
        var sk = cfg["Storage:S3:SecretKey"]!;
        
        if (string.IsNullOrWhiteSpace(ep))
            throw new InvalidOperationException("Storage:Endpoint mangler i konfigurationen (fx http://minio:9000).");
        if (string.IsNullOrWhiteSpace(ak) || string.IsNullOrWhiteSpace(sk))
            throw new InvalidOperationException("Storage:AccessKey/SecretKey mangler i konfigurationen.");

        return new AmazonS3Client(
            ak, sk,
            new AmazonS3Config {
                ServiceURL = ep,
                ForcePathStyle = true, // vigtigt for MinIO
                AuthenticationRegion = "us-east-1"
            });
    }
}