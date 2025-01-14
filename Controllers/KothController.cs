using Microsoft.AspNetCore.Mvc;
using KothBackend.Services;
using KothBackend.Models;
using System.Text.Json;
using System.Xml.Linq;

namespace KothBackend.Controllers
{
    [ApiController]
    [Route("api")]
    public class KothController : ControllerBase
    {
        private readonly IMongoDbService _mongoService;
        private const string API_KEY = "official_testapikey";

        public KothController(IMongoDbService mongoService)
        {
            _mongoService = mongoService;
        }

        private bool ValidateApiKey()
        {
            //if (!Request.Headers.TryGetValue("X-AUTH-TOKEN", out var apiKey))
            //{
            //    Console.WriteLine("No API key provided");
            //    throw new UnauthorizedAccessException("Invalid API key");
            //}

            //Console.WriteLine($"Received API key: {apiKey}");

            //if (apiKey != API_KEY)
            //{
            //    Console.WriteLine("API key mismatch");
            //    throw new UnauthorizedAccessException("Invalid API key");
            //}
            return true;
        }

        [HttpGet("profile")]
        public async Task<ActionResult<PlayerProfile>> GetProfile([FromQuery] string bohemiaUID)
        {
            ValidateApiKey();
            var profile = await _mongoService.GetProfile(bohemiaUID);
            return Ok(profile);
        }

        [HttpPost("profile")]
        public async Task<IActionResult> UpdateProfile()
        {
            ValidateApiKey();

            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            // Log the raw incoming data
            Console.WriteLine($"Raw incoming data: {body}");

            var parts = body.Split('=', 2);

            if (parts.Length < 2)
            {
                Console.WriteLine("Invalid request format - no content parameter found");
                return BadRequest("Invalid request format");
            }

            try
            {
                var newProfile = JsonSerializer.Deserialize<PlayerProfile>(parts[1]);
                if (newProfile == null)
                {
                    Console.WriteLine("Failed to deserialize profile");
                    return BadRequest("Invalid JSON format");
                }

                await _mongoService.UpdateProfile(newProfile);
                return Ok(new { status = "success", message = "Profile updated" });
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"JSON error: {ex.Message}");
                return BadRequest($"Invalid JSON format: {ex.Message}");
            }
        }

        [HttpPost("profiles")]
        public async Task<IActionResult> UpdateProfiles()
        {
            ValidateApiKey();

            // Fetch all profiles from the database
            var dbProfiles = await _mongoService.GetAllProfiles();

            // Wrap profiles in ListPlayerProfile to match the response structure
            var response = new ListPlayerProfile
            {
                m_list = dbProfiles
            };

            return Ok(response); // Return the profiles
        }

        [HttpGet("stats/playerstats")]
        public async Task<ActionResult<PlayerStats>> GetPlayerStats([FromQuery] string bohemiaUID)
        {
            ValidateApiKey();
            var stats = await _mongoService.GetPlayerStats(bohemiaUID);
            return Ok(stats);
        }

        [HttpPost("stats/playersstats")]
        public async Task<IActionResult> UpdatePlayersStats()
        {
            ValidateApiKey();

            // Fetch all stats from the database
            var dbStats = await _mongoService.GetAllPlayerStats();

            // Wrap stats in ListPlayerStats to match the response structure
            var response = new ListPlayerStats
            {
                m_list = dbStats
            };

            return Ok(response); // Return the stats
        }

        [HttpPost("preset/{playerUID}")]
        public async Task<IActionResult> UpdatePreset(string playerUID)
        {
            ValidateApiKey();

            // Read the body content, whether it is JSON or x-www-form-urlencoded
            string rawBody;
            using (var reader = new StreamReader(Request.Body))
            {
                rawBody = await reader.ReadToEndAsync();
            }

            // Parse the body content
            PlayerPreset preset;
            try
            {
                // If Content-Type is x-www-form-urlencoded, extract the "content" key
                if (Request.ContentType != null && Request.ContentType.Contains("application/x-www-form-urlencoded"))
                {
                    var parsedData = rawBody.Split('=', 2);
                    if (parsedData.Length < 2 || parsedData[0] != "content")
                    {
                        return BadRequest(new { error = true, errorReason = "Invalid form-encoded payload" });
                    }

                    rawBody = Uri.UnescapeDataString(parsedData[1]); // Decode the JSON part
                }

                // Deserialize the JSON into a PlayerPreset object
                preset = JsonSerializer.Deserialize<PlayerPreset>(rawBody);
                if (preset == null)
                {
                    throw new JsonException("Deserialized object is null");
                }
            }
            catch (JsonException ex)
            {
                return BadRequest(new { error = true, errorReason = $"Invalid JSON format: {ex.Message}" });
            }

            // Fetch the player's profile
            var profile = await _mongoService.GetProfile(playerUID);
            if (profile == null)
            {
                return NotFound(new { error = true, errorReason = "Player profile not found" });
            }

            // Update the player's preset and save it
            profile.m_playerPresets = new List<PlayerPreset> { preset };
            await _mongoService.UpdateProfile(profile);

            return Ok(new { status = "success", message = "Preset updated" });
        }

        [HttpGet("activeBans")]
        public async Task<ActionResult<ListPlayerBan>> GetActiveBans()
        {
            ValidateApiKey();
            var bans = await _mongoService.GetActiveBans();
            return Ok(new ListPlayerBan { m_list = bans });
        }

        [HttpGet("bonus")]
        public async Task<ActionResult<BonusCodeResponse>> GetBonus([FromQuery] string bohemiaUID)
        {
            // Validate API key
            ValidateApiKey();

            // Get bonus code from MongoDB service
            var bonus = await _mongoService.GetBonusCode(bohemiaUID);
            return Ok(bonus);
        }

        [HttpPost("bonusCode")]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult<BonusCode>> UseBonus([FromForm] string content)
        {
            try
            {
                ValidateApiKey();

                // Parse the content string which contains JSON
                var requestData = JsonSerializer.Deserialize<BonusCodeRequest>(content);

                if (requestData == null || string.IsNullOrEmpty(requestData.Code) || string.IsNullOrEmpty(requestData.PlayerUID))
                {
                    return BadRequest(new { message = "Invalid request format" });
                }

                var bonusCode = await _mongoService.UseBonusCode(requestData.Code, requestData.PlayerUID);

                if (bonusCode == null)
                {
                    return NotFound(new { message = "Invalid or expired bonus code, or code already used by player" });
                }

                return Ok(bonusCode);
            }
            catch (JsonException)
            {
                return BadRequest(new { message = "Invalid JSON format in content" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("admin/createbonus")]
        public async Task<IActionResult> CreateBonusCode([FromBody] BonusCodeCreateRequest request)
        {
            ValidateApiKey();

            try
            {
                await _mongoService.CreateBonusCode(
                    request.Code,
                    request.Name,
                    request.Multiplier,  // Now matches the string type
                    request.ValidDays
                );
                return Ok(new { status = "success", message = "Bonus code created" });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error creating bonus code: {ex.Message}");
            }
        }

        [HttpPost("stats/votemap")]
        public IActionResult RecordVoteMap([FromQuery] string scenario, [FromQuery] int votes)
        {
            ValidateApiKey();
            return Ok(new { status = "success" });
        }

        [HttpPost("stats/votemapwinner")]
        public IActionResult RecordVoteMapWinner([FromQuery] string scenario)
        {
            ValidateApiKey();
            return Ok(new { status = "success" });
        }

        [HttpPost("stats/teamwinner")]
        public IActionResult RecordTeamWinner([FromQuery] string team)
        {
            ValidateApiKey();
            return Ok(new { status = "success" });
        }
    }
}