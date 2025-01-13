using Microsoft.Extensions.Options;
using KothBackend.Configuration;
using KothBackend.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace KothBackend.Services
{
    public class MongoDbService : IMongoDbService
    {
        private readonly IMongoCollection<PlayerProfile> _profiles;
        private readonly IMongoCollection<PlayerStats> _stats;
        private readonly IMongoCollection<BsonDocument> _bans;
        private readonly IMongoCollection<BonusCode> _bonusCodes;

        public MongoDbService(IOptions<MongoDbSettings> settings)
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            var database = client.GetDatabase(settings.Value.DatabaseName);

            _profiles = database.GetCollection<PlayerProfile>("profiles");
            _stats = database.GetCollection<PlayerStats>("stats");
            _bans = database.GetCollection<BsonDocument>("bans");
            _bonusCodes = database.GetCollection<BonusCode>("bonus_codes");
        }

        public async Task<PlayerProfile> GetProfile(string playerUID)
        {
            var profile = await _profiles.Find(p => p.m_playerUID == playerUID).FirstOrDefaultAsync();
            if (profile == null)
            {
                profile = new PlayerProfile { m_playerUID = playerUID };
                await _profiles.InsertOneAsync(profile);
            }
            return profile;
        }

        public async Task UpdateProfile(PlayerProfile newProfile)
        {
            var existingProfile = await GetProfile(newProfile.m_playerUID);

            // Merge logic
            if (string.IsNullOrEmpty(newProfile.m_playerName)) newProfile.m_playerName = existingProfile.m_playerName;
            if (string.IsNullOrEmpty(newProfile.m_profileName)) newProfile.m_profileName = existingProfile.m_profileName;
            if (string.IsNullOrEmpty(newProfile.m_platformName)) newProfile.m_platformName = existingProfile.m_platformName;
            if (string.IsNullOrEmpty(newProfile.m_machineName)) newProfile.m_machineName = existingProfile.m_machineName;
            if (string.IsNullOrEmpty(newProfile.m_adapterName)) newProfile.m_adapterName = existingProfile.m_adapterName;

            if (newProfile.m_money < 0) newProfile.m_money = existingProfile.m_money;
            if (newProfile.m_level < 0) newProfile.m_level = existingProfile.m_level;
            if (newProfile.m_xp < 0) newProfile.m_xp = existingProfile.m_xp;

            newProfile.m_kills = Math.Max(newProfile.m_kills, existingProfile.m_kills);
            newProfile.m_deaths = Math.Max(newProfile.m_deaths, existingProfile.m_deaths);
            newProfile.m_friendlyKills = Math.Max(newProfile.m_friendlyKills, existingProfile.m_friendlyKills);

            var mergedUnlockedItems = new HashSet<string>(existingProfile.m_unlockedItems);
            mergedUnlockedItems.UnionWith(newProfile.m_unlockedItems);
            newProfile.m_unlockedItems = mergedUnlockedItems.ToList();

            if (newProfile.m_playerPresets.Count == 0)
            {
                newProfile.m_playerPresets = existingProfile.m_playerPresets;
            }

            await _profiles.ReplaceOneAsync(p => p.m_playerUID == newProfile.m_playerUID, newProfile, new ReplaceOptions { IsUpsert = true });
        }

        public async Task UpdateProfiles(List<PlayerProfile> profiles)
        {
            var bulkOps = profiles.Select(profile =>
                new ReplaceOneModel<PlayerProfile>(
                    Builders<PlayerProfile>.Filter.Eq(p => p.m_playerUID, profile.m_playerUID),
                    profile)
                { IsUpsert = true });

            await _profiles.BulkWriteAsync(bulkOps);
        }

        public async Task<PlayerStats> GetPlayerStats(string playerUID)
        {
            var stats = await _stats.Find(s => s.m_playerUID == playerUID).FirstOrDefaultAsync();
            if (stats == null)
            {
                stats = new PlayerStats { m_playerUID = playerUID };
                await _stats.InsertOneAsync(stats);
            }
            return stats;
        }

        public async Task UpdatePlayerStats(List<PlayerStats> statsList)
        {
            var bulkOps = statsList.Select(stats =>
                new ReplaceOneModel<PlayerStats>(
                    Builders<PlayerStats>.Filter.Eq(s => s.m_playerUID, stats.m_playerUID),
                    stats)
                { IsUpsert = true });

            await _stats.BulkWriteAsync(bulkOps);
        }

        public async Task<List<string>> GetActiveBans()
        {
            var bans = await _bans.Find(new BsonDocument()).ToListAsync();
            return bans.Select(b => b["playerUID"].AsString).ToList();
        }

        public async Task AddBan(string playerUID)
        {
            await _bans.InsertOneAsync(new BsonDocument("playerUID", playerUID));
        }

        public async Task<BonusCodeResponse> GetBonusCode(string playerUID)
        {
            var bonusCode = await _bonusCodes
                .Find(b => !b.UsedByPlayers.Contains(playerUID) && DateTime.Parse(b.DateEnd) > DateTime.UtcNow)
                .FirstOrDefaultAsync();

            if (bonusCode == null)
            {
                return new BonusCodeResponse
                {
                    error = true,
                    errorReason = "No bonus code found"
                };
            }

            return new BonusCodeResponse
            {
                name = bonusCode.Name,
                code = bonusCode.Code,
                playerUID = playerUID,
                multiplier = bonusCode.Multiplier,
                dateEnd = bonusCode.DateEnd,
                error = false
            };
        }

        public async Task<BonusCodeResponse> UseBonusCode(string code, string playerUID)
        {
            var bonusCode = await _bonusCodes
                .Find(b => b.Code == code && DateTime.Parse(b.DateEnd) > DateTime.UtcNow)
                .FirstOrDefaultAsync();

            if (bonusCode == null)
            {
                return new BonusCodeResponse
                {
                    error = true,
                    errorReason = "Invalid or expired bonus code"
                };
            }

            if (bonusCode.UsedByPlayers.Contains(playerUID))
            {
                return new BonusCodeResponse
                {
                    error = true,
                    errorReason = "You have already used this bonus code"
                };
            }

            // Add player to used list
            var update = Builders<BonusCode>.Update.AddToSet(b => b.UsedByPlayers, playerUID);
            await _bonusCodes.UpdateOneAsync(b => b.Code == code, update);

            return new BonusCodeResponse
            {
                name = bonusCode.Name,
                code = bonusCode.Code,
                playerUID = playerUID,
                multiplier = bonusCode.Multiplier,
                dateEnd = bonusCode.DateEnd,
                error = false
            };
        }

        public async Task CreateBonusCode(string code, string name, string multiplier, int validDays)
        {
            var bonusCode = new BonusCode
            {
                Code = code,
                Name = name,
                Multiplier = multiplier,
                DateEnd = DateTime.UtcNow.AddDays(validDays).ToString("yyyy-MM-dd HH:mm:ss"),
                IsUsed = false,
                UsedByPlayers = new HashSet<string>()
            };

            await _bonusCodes.InsertOneAsync(bonusCode);
        }
    }
}