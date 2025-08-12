using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using Xabe.FFmpeg;



namespace ReactorApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        private readonly FirestoreDb _db;

        public HomeController(FirestoreDb db)
        {
            _db = db;
        }

        [FirestoreData]
        public struct VideoMetaData()
        {
            [FirestoreProperty]
            public int VideoId { get; set; }
            [FirestoreProperty]

            public string FilePath { get; set; }
            [FirestoreProperty]
            public string videoName { get; set; }
            [FirestoreProperty]
            public string ThumbnailPath { get; set; }

        }


        private async Task<MemoryStream> GenerateThumbnailFromVideo(IFormFile videoFile)
        {
            // Save video temporarily to disk
            var tempVideoPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + Path.GetExtension(videoFile.FileName));
            using (var stream = new FileStream(tempVideoPath, FileMode.Create))
            {
                await videoFile.CopyToAsync(stream);
            }

            var tempThumbnailPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".jpg");

            // Build FFmpeg command arguments to extract a frame at 1 second
            string arguments = $"-ss 00:00:01 -i \"{tempVideoPath}\" -frames:v 1 \"{tempThumbnailPath}\"";

            var ffmpegPath = @"C:\ffmpeg\bin\ffmpeg.exe"; // Change to your ffmpeg.exe path

            var processInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using (var process = new Process { StartInfo = processInfo })
            {
                process.Start();

                // Optionally, read output and error for logging/debugging
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    // FFmpeg failed, handle error
                    System.IO.File.Delete(tempVideoPath);
                    throw new Exception($"FFmpeg exited with code {process.ExitCode}. Error: {error}");
                }
            }

            // Read the generated thumbnail into memory
            var memoryStream = new MemoryStream(await System.IO.File.ReadAllBytesAsync(tempThumbnailPath));

            // Clean up temp files
            System.IO.File.Delete(tempVideoPath);
            System.IO.File.Delete(tempThumbnailPath);

            return memoryStream;
        }
        private async Task<string> UploadStreamToFirebaseStorage(Stream stream, string folder, string fileName)
        {
            var storage = StorageClient.Create();
            var bucketName = "nuclearreactor-2b876.firebasestorage.app";

            var objectName = $"{folder}{fileName}";

            stream.Position = 0; // reset stream

            await storage.UploadObjectAsync(bucketName, objectName, "image/jpeg", stream);

            return $"{objectName}";
        }

        [HttpPost]
        [Route("UploadVideo")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadVideo([FromForm] IFormFile Video, [FromForm] string VideoName)
        {
            CollectionReference VideoCollection = _db.Collection("videos");
            FirebaseStorageService UploadTask = new FirebaseStorageService();
            VideoMetaData Videodata = new VideoMetaData();
            var collection = _db.Collection("videos");
            var snapshop = await collection.GetSnapshotAsync();
            var videos = snapshop.Documents.Where(d => d.Exists).Select(d => d.ConvertTo<VideoMetaData>()).ToList();

            MemoryStream thumbnailStream = await GenerateThumbnailFromVideo(Video);

         



            if (Video == null)
                return BadRequest("No files uploaded");

            if (!VideoName.Equals(null))
            {
                var thumbnailUrl = await UploadStreamToFirebaseStorage(thumbnailStream, "thumbnails/", "thumb_" + Path.GetFileNameWithoutExtension(Video.FileName) + ".jpg");

                Videodata.VideoId = videos.Any() ? videos.Max(v => v.VideoId) + 1 : 0;
                Videodata.FilePath = $"videos/{Video.FileName}";
                Videodata.videoName = VideoName;
                Videodata.ThumbnailPath = thumbnailUrl;
            }
            else
            {
                return BadRequest("No Video Name");
            }

            using var stream = Video.OpenReadStream();
            await UploadTask.UploadVideoAsync(stream, $"Videos/{Video.FileName}");

            await collection.AddAsync(Videodata);
            return Ok("Upload Successfully");
        }

        [HttpGet]
        [Route("Videos")]
        public async Task<ActionResult> GetVideo()
        {
            var collection = _db.Collection("videos");
            var snapshot = await collection.GetSnapshotAsync();
            var videos = snapshot.Documents.Where(d => d.Exists).Select(d => d.ConvertTo<VideoMetaData>()).ToList();

            return Ok(videos);

        }
    }

}