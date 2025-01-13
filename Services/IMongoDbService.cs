using KothBackend.Models;

namespace KothBackend.Services
{
    public interface IMongoDbService
    {
        Task<PlayerProfile> GetProfile(string playerUID);
        Task UpdateProfile(PlayerProfile profile);
        Task UpdateProfiles(List<PlayerProfile> profiles);
        Task<PlayerStats> GetPlayerStats(string playerUID);
        Task UpdatePlayerStats(List<PlayerStats> stats);
        Task<List<string>> GetActiveBans();
        Task AddBan(string playerUID);
        Task<BonusCodeResponse> GetBonusCode(string playerUID);
        Task<BonusCodeResponse> UseBonusCode(string code, string playerUID);
        Task CreateBonusCode(string code, string name, double multiplier, int validDays);
    }
}