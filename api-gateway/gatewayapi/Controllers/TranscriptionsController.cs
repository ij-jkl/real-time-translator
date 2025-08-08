using gatewayapi.Services;
using Microsoft.AspNetCore.Mvc;

namespace gatewayapi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TranscriptionsController : ControllerBase
    {
        private readonly ITranscriptionLogService _transcriptionLogService;

        public TranscriptionsController(ITranscriptionLogService transcriptionLogService)
        {
            _transcriptionLogService = transcriptionLogService;
        }

        [HttpGet]
        public async Task<IActionResult> GetTranscriptions(
            [FromQuery] string? sessionId = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] int skip = 0,
            [FromQuery] int take = 50)
        {
            try
            {
                var transcriptions = await _transcriptionLogService.GetTranscriptionsAsync(
                    sessionId, fromDate, toDate, skip, take);

                return Ok(transcriptions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve transcriptions" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTranscription(int id)
        {
            try
            {
                var transcription = await _transcriptionLogService.GetTranscriptionByIdAsync(id);
                
                if (transcription == null)
                    return NotFound();

                return Ok(transcription);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve transcription" });
            }
        }
    }
}
