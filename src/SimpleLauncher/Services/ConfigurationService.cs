using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SimpleLauncher.Domain.Abstractions;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SimpleLauncher.Services
{
    public class ConfigurationService : IConfigurationService
    {
        private const string CannotReloadConfigErrorMessage = "Cannot reload configuration after change.";
        private readonly string _configFilePath;
        private readonly IConfigurationRoot _configurationRoot;

        public ConfigurationService(IConfiguration configuration, 
            string configFilePath)
        {
            _configurationRoot = configuration as IConfigurationRoot 
                ?? throw new ArgumentException("Configuration is not an IConfigurationRoot");
            _configFilePath = !string.IsNullOrWhiteSpace(configFilePath)
                ? configFilePath
                : throw new ArgumentException("configuration file path is empty");
        }

        public bool AddValueToArray(string sectionPath, string value)
        {
            var json = File.ReadAllText(_configFilePath);
            var jsonNode = JsonNode.Parse(json) ?? new JsonObject();

            var parts = sectionPath.Split(':');
            JsonNode? current = jsonNode;
            for (int i = 0; i < parts.Length - 1; i++)
            {
                if (current is null)
                    continue;
                if (current[parts[i]] is null)
                    current[parts[i]] = new JsonObject();

                current = current[parts[i]];
            }

            var lastPart = parts[^1];
            if (current is not null && 
                current[lastPart] is JsonArray array)
            {
                if (array.Where(arr => arr.ToJsonString().Equals($"\"{value}\"")).Any())
                    return false;
                array.Add(value);
            }
            else
            {
                current[lastPart] = new JsonArray(value);
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            var newJson = jsonNode.ToJsonString(options);
            File.WriteAllText(_configFilePath, newJson);
            
            return ReloadConfiguration();
        }

        public void SaveValue(string section, string key, string value)
        {
            var json = File.ReadAllText(_configFilePath);
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            var configDictionary = ConvertToDictionary(root);

            SetNestedValue(configDictionary, $"{section}:{key}", value);

            var options = new JsonSerializerOptions { WriteIndented = true };
            var newJson = JsonSerializer.Serialize(configDictionary, options);
            File.WriteAllText(_configFilePath, newJson);
            ReloadConfiguration();
        }
        private bool ReloadConfiguration()
        {
            try
            {
                _configurationRoot.Reload();
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, CannotReloadConfigErrorMessage);
                return false;
            }
            return true;
        }

        private Dictionary<string, object> ConvertToDictionary(JsonElement element)
        {
            var dict = new Dictionary<string, object>();

            foreach (var property in element.EnumerateObject())
            {
                dict[property.Name] = property.Value.ValueKind switch
                {
                    JsonValueKind.Object => ConvertToDictionary(property.Value),
                    JsonValueKind.Array => ConvertToList(property.Value),
                    JsonValueKind.String => property.Value.GetString(),
                    JsonValueKind.Number => property.Value.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null,
                    _ => property.Value.ToString()
                };
            }

            return dict;
        }

        private List<object> ConvertToList(JsonElement element)
        {
            var list = new List<object>();

            foreach (var item in element.EnumerateArray())
            {
                list.Add(item.ValueKind switch
                {
                    JsonValueKind.Object => ConvertToDictionary(item),
                    JsonValueKind.Array => ConvertToList(item),
                    JsonValueKind.String => item.GetString(),
                    JsonValueKind.Number => item.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null,
                    _ => item.ToString()
                });
            }

            return list;
        }

        private void SetNestedValue(Dictionary<string, object> dict, string path, object value)
        {
            var parts = path.Split(':');
            var current = dict;

            for (int i = 0; i < parts.Length - 1; i++)
            {
                if (!current.ContainsKey(parts[i]))
                {
                    current[parts[i]] = new Dictionary<string, object>();
                }

                current = current[parts[i]] as Dictionary<string, object>;
            }

            current[parts[^1]] = value;
        }
    }
}
