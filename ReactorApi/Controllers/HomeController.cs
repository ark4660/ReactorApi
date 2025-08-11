using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations.Schema;



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
            public string videoName { get; set; }

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
            
            

            if (Video == null)
                return BadRequest("No files uploaded");

            if (!VideoName.Equals(null))
            {
                
                Videodata.VideoId = videos.Any() ? videos.Max(v => v.VideoId) + 1: 0;
                Videodata.FilePath = $"videos/{Video.FileName}";
                Videodata.videoName = VideoName;
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
        public ActionResult GetVideo()
        {
            return null;
        }
    }
    
}
