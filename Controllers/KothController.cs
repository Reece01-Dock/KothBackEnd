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
            if (!Request.Headers.TryGetValue("X-AUTH-TOKEN", out var apiKey) || apiKey != API_KEY)
            {
                throw new UnauthorizedAccessException("Invalid API key");
            }
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

            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            var parts = body.Split('=', 2);

            if (parts.Length < 2)
                return BadRequest("Invalid request format");

            try
            {
                var profiles = JsonSerializer.Deserialize<ListPlayerProfile>(parts[1]);
                if (profiles == null)
                    return BadRequest("Invalid JSON format");

                await _mongoService.UpdateProfiles(profiles.m_list);
                return Ok(new { status = "success", message = $"Updated {profiles.m_list.Count} profiles" });
            }
            catch (JsonException)
            {
                return BadRequest("Invalid JSON format");
            }
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

            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            var parts = body.Split('=', 2);

            if (parts.Length < 2)
                return BadRequest("Invalid request format");

            try
            {
                var statsList = JsonSerializer.Deserialize<ListPlayerStats>(parts[1]);
                if (statsList == null)
                    return BadRequest("Invalid JSON format");

                await _mongoService.UpdatePlayerStats(statsList.m_list);
                return Ok(new { status = "success", message = $"Updated stats for {statsList.m_list.Count} players" });
            }
            catch (JsonException)
            {
                return BadRequest("Invalid JSON format");
            }
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
            ValidateApiKey();
            var response = await _mongoService.GetBonusCode(bohemiaUID);
            return Ok(response);
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