using KothBackend.Models;

namespace KothBackend.Services
{
    public interface IMongoDbService
    {
        Task<PlayerProfile> GetProfile(string playerUID);
        Task<List<PlayerProfile>> GetAllProfiles(); // New method
        Task UpdateProfile(PlayerProfile profile);
        Task UpdateProfiles(List<PlayerProfile> profiles);
        Task<PlayerStats> GetPlayerStats(string playerUID);
        Task<List<PlayerStats>> GetAllPlayerStats(); // New method
        Task UpdatePlayerStats(List<PlayerStats> stats);
        Task<List<string>> GetActiveBans();
        Task AddBan(string playerUID);
        Task<BonusCode?> GetBonusCode(string playerUID);
        Task<BonusCode?> UseBonusCode(string code, string playerUID);
        Task CreateBonusCode(string code, string name, string multiplier, int validDays);
    }

}