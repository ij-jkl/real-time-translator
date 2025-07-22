using Microsoft.AspNetCore.Mvc;
using System.Net.Http;

namespace gateway.Controllers
{
    [ApiController]
    [Route("/")]
    public class TranscribeController : ControllerBase
    {
        [HttpPost("transcribe_from_audio")]
        public async Task<IActionResult> TranscribeFromAudio()
        {
            var form = await Request.ReadFormAsync();
            var content = new MultipartFormDataContent();
            foreach (var field in form)
            {
                if (field.Value.Count == 1)
                    content.Add(new StringContent(field.Value), field.Key);
            }
            var file = form.Files["audioFile"];
            if (file != null)
            {
                content.Add(new StreamContent(file.OpenReadStream()), "audioFile", file.FileName);
            }
            using var httpClient = new HttpClient();
            var backendResponse = await httpClient.PostAsync("http://localhost:8000/transcribe_from_audio", content);
            var responseContent = await backendResponse.Content.ReadAsStringAsync();
            return Content(responseContent, backendResponse.Content.Headers.ContentType?.ToString() ?? "application/json");
        }
    }
}
