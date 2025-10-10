namespace SimpleLauncher.Domain.Abstractions
{
    public interface IConfigurationService
    {
        public void AddValueToArray(string section, string value);
        public void SaveValue(string section, string key, string value);
    }
}