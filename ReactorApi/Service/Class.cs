using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;

public class FirebaseStorageService
{
    private readonly StorageClient _storageClient;
    private readonly string _bucketName;

    // Initialize once with your credentials and bucket name
    public FirebaseStorageService()
    {
        // Path to your service account JSON
        var jsonPath = "Service\\nuclearreactor-2b876-firebase-adminsdk-fbsvc-a01871a38f.json";

        GoogleCredential credential = GoogleCredential.FromFile(jsonPath);
        _storageClient = StorageClient.Create(credential);

        // Your bucket name (usually project-id.appspot.com)
        _bucketName = "nuclearreactor-2b876.firebasestorage.app";
    }

    // Upload method only needs the video stream and target filename
    public async Task UploadVideoAsync(Stream videoStream, string destinationFileName)
    {
        await _storageClient.UploadObjectAsync(
            _bucketName,
            destinationFileName,
            null,
            videoStream
        );
    }
}
