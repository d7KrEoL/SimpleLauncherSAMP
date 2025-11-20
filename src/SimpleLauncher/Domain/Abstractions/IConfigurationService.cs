using SimpleLauncher.Domain.Models;

namespace SimpleLauncher.Domain.Abstractions
{
    public interface IConfigurationService
    {
        const string DefaultConfigFileName = "settings.json";
        const string ClientListSection = "ClientList";
        const string OwnerInfoSection = "OwnerInfo";
        const string FriendListSection = "FriendList";
        const string ServerListSection = "ServerList";
        const string MonitoringSourcesSection = "MonitoringSources";
        bool AddValueToArray(string section, string value);
        bool EditValueInArrayOrAdd(string sectionPath,
            string oldValue,
            string newValue);
        bool AddGameClient(GameClient client);
        bool EditGameClient(string legacyName, GameClient client);
        bool DeleteGameClient(string clientName);
        void SaveValue(string section, string key, string value);
    }
}