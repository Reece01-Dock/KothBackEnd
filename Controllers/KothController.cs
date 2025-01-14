using Microsoft.AspNetCore.Mvc;
using KothBackend.Services;
using KothBackend.Models;
using System.Text.Json;

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
                // Log the JSON part
                Console.WriteLine($"JSON content: {parts[1]}");

                var newProfile = JsonSerializer.Deserialize<PlayerProfile>(parts[1]);
                if (newProfile == null)
                {
                    Console.WriteLine("Failed to deserialize profile");
                    return BadRequest("Invalid JSON format");
                }

                // Log the deserialized profile
                Console.WriteLine($"Deserialized profile - UID: {newProfile.m_playerUID}");
                Console.WriteLine($"Player Name: {newProfile.m_playerName}");
                Console.WriteLine($"Level: {newProfile.m_level}, XP: {newProfile.m_xp}, Money: {newProfile.m_money}");
                Console.WriteLine($"Kills: {newProfile.m_kills}, Deaths: {newProfile.m_deaths}");

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
        public async Task<IActionResult> UpdatePreset(string playerUID, [FromBody] PlayerPreset preset)
        {
            ValidateApiKey();

            var profile = await _mongoService.GetProfile(playerUID);
            if (profile == null)
            {
                return NotFound("Player profile not found");
            }

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

            // Ensure the bohemiaUID parameter is provided
            if (string.IsNullOrWhiteSpace(bohemiaUID))
                return BadRequest(new BonusCodeResponse
                {
                    error = true,
                    errorReason = "bohemiaUID is required"
                });

            try
            {
                // Fetch the bonus code for the player
                var bonusCode = await _mongoService.GetBonusCode(bohemiaUID);

                if (bonusCode == null)
                {
                    // No valid bonus code available
                    return Ok(new BonusCodeResponse
                    {
                        error = true,
                        errorReason = "No bonus code found"
                    });
                }

                // Return the bonus code details
                return Ok(new BonusCodeResponse
                {
                    name = bonusCode.name,
                    code = bonusCode.code,
                    playerUID = bonusCode.playerUID,
                    multiplier = bonusCode.multiplier,
                    dateEnd = bonusCode.dateEnd,
                    error = false,
                    errorReason = string.Empty
                });
            }
            catch (Exception ex)
            {
                // Log the exception and return an error response
                Console.WriteLine($"Error fetching bonus code for UID {bohemiaUID}: {ex.Message}");
                return StatusCode(500, new BonusCodeResponse
                {
                    error = true,
                    errorReason = "An error occurred while fetching the bonus code"
                });
            }
        }

        [HttpPost("bonusCode")]
        public async Task<ActionResult<BonusCodeResponse>> UseBonusCode()
        {
            ValidateApiKey();

            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            var parts = body.Split('=', 2);

            if (parts.Length < 2)
                return BadRequest("Invalid request format");

            try
            {
                var data = JsonSerializer.Deserialize<Dictionary<string, string>>(parts[1]);
                if (data == null || !data.ContainsKey("code") || !data.ContainsKey("playerUID"))
                    return BadRequest("Invalid JSON format");

                var response = await _mongoService.UseBonusCode(data["code"], data["playerUID"]);
                return Ok(response);
            }
            catch (JsonException)
            {
                return BadRequest("Invalid JSON format");
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