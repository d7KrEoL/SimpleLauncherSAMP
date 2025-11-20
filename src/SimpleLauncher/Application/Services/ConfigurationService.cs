using Microsoft.Extensions.Configuration;
using SimpleLauncher.Domain.Abstractions;
using SimpleLauncher.Domain.Models;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SimpleLauncher.Application.Services
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

        public bool AddValueToArray(string sectionPath, 
            string value)
        {
            var json = File.ReadAllText(_configFilePath);
            var jsonNode = JsonNode.Parse(json) ?? new JsonObject();

            var (current, lastPart) = NavigateToSection(jsonNode, sectionPath);
            if (lastPart is null)
                return false;
            if (current is not null && 
                current[lastPart] is JsonArray array)
            {
                var elements = array
                    .Where(arr => arr?
                        .ToJsonString()
                        .Equals($"\"{value}\"") ?? false);
                if (!elements.Any())
                    return false;
                array.Add(value);
            }
            else
                current?[lastPart] = new JsonArray(value);

            var options = new JsonSerializerOptions { WriteIndented = true };
            var newJson = jsonNode.ToJsonString(options);
            File.WriteAllText(_configFilePath, newJson);
            
            return ReloadConfiguration();
        }

        public bool EditValueInArrayOrAdd(string sectionPath,
            string oldValue,
            string newValue)
        {

            var json = File.ReadAllText(_configFilePath);
            var jsonNode = JsonNode.Parse(json) ?? new JsonObject();
            var (current, lastPart) = NavigateToSection(jsonNode, sectionPath);
            if (lastPart is null)
                return false;
            if (current is not null &&
                current[lastPart] is JsonArray array)
            {
                var elements = array
                    .Where(arr => arr?
                        .ToJsonString()
                        .Equals($"\"{oldValue}\"") ?? false);
                if (!elements.Any())
                    array.Add(newValue);
                else
                {
                    for (int i = 0; i < array.Count; i++)
                    {
                        if (array[i]?.ToJsonString() == $"\"{oldValue}\"")
                        {
                            array[i] = newValue;
                            break;
                        }
                    }
                }
            }
            else
            {
                current?[lastPart] = new JsonArray(newValue);
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
        public bool AddGameClient(GameClient client)
        {
            var json = File.ReadAllText(_configFilePath);
            var jsonNode = JsonNode.Parse(json) ?? new JsonObject();
            var (current, lastPart) = NavigateToSection(jsonNode, IConfigurationService.ClientListSection);
            if (lastPart is null || current is null)
                return false;
            var clientObj = new JsonObject
            {
                ["Version"] = client.Version,
                ["Path"] = client.Path,
                ["ClientExecutableName"] = client.ClientExecutableName,
                ["GameExecutableName"] = client.GameExecutableName
            };
            if (current[lastPart] is JsonObject clientListObj)
            {
                clientListObj[client.Name] = clientObj;
            }
            else
            {
                var newClientListObj = new JsonObject
                {
                    [client.Name] = clientObj
                };
                current[lastPart] = newClientListObj;
            }
            var options = new JsonSerializerOptions { WriteIndented = true };
            var newJson = jsonNode.ToJsonString(options);
            File.WriteAllText(_configFilePath, newJson);
            return ReloadConfiguration();
        }
        public bool EditGameClient(string legacyName, GameClient client)
        {
            var json = File.ReadAllText(_configFilePath);
            var jsonNode = JsonNode.Parse(json) ?? new JsonObject();
            var (current, lastPart) = NavigateToSection(jsonNode, IConfigurationService.ClientListSection);
            if (lastPart is null || current is null)
                return false;
            if (current[lastPart] is JsonObject clientListObj)
            {
                string? foundKey = null;
                foreach (var kvp in clientListObj)
                {
                    var key = kvp.Key;
                    var value = kvp.Value as JsonObject;
                    if (value == null)
                        continue;

                    var nameMatch = key.Equals(legacyName, StringComparison.OrdinalIgnoreCase)
                        || key.Equals(client.Name, StringComparison.OrdinalIgnoreCase);

                    var pathMatch = value["Path"]?
                        .ToString()
                        .Equals(client.Path, StringComparison.OrdinalIgnoreCase) == true;

                    if (nameMatch || pathMatch)
                    {
                        foundKey = key;
                        break;
                    }
                }
                var newClientObj = new JsonObject
                {
                    ["Version"] = client.Version,
                    ["Path"] = client.Path,
                    ["ClientExecutableName"] = client.ClientExecutableName,
                    ["GameExecutableName"] = client.GameExecutableName
                };
                if (foundKey is null)
                    clientListObj[client.Name] = newClientObj;
                else
                    clientListObj[foundKey] = newClientObj;
            }
            else
                return false;

            var options = new JsonSerializerOptions { WriteIndented = true };
            var newJson = jsonNode.ToJsonString(options);
            File.WriteAllText(_configFilePath, newJson);
            return ReloadConfiguration();
        }
        public bool DeleteGameClient(string clientName)
        {
            var json = File.ReadAllText(_configFilePath);
            var jsonNode = JsonNode.Parse(json) ?? new JsonObject();
            var (current, lastPart) = NavigateToSection(jsonNode, IConfigurationService.ClientListSection);
            if (lastPart is null || current is null)
                return false;
            if (current[lastPart] is JsonObject clientListObj)
            {
                var keyToRemove = clientListObj
                    .Where(kvp => kvp.Key.Equals(clientName, StringComparison.OrdinalIgnoreCase))
                    .Select(kvp => kvp.Key)
                    .FirstOrDefault();

                if (keyToRemove != null)
                    clientListObj.Remove(keyToRemove);
                else
                    return false;
            }
            else if (current[lastPart] is JsonArray array)
            {
                for (int i = array.Count - 1; i >= 0; i--)
                {
                    var element = array[i] as JsonObject;
                    if (element?["Name"]?
                        .ToString()
                        .Equals(clientName, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        array.RemoveAt(i);
                        break;
                    }
                }
            }
            else
                return false;
            var options = new JsonSerializerOptions { WriteIndented = true };
            var newJson = jsonNode.ToJsonString(options);
            File.WriteAllText(_configFilePath, newJson);
            return ReloadConfiguration();
        }
        private (JsonNode? current, string? lastPart) NavigateToSection(JsonNode jsonNode,
            string sectionPath)
        {
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
            return (current, lastPart);
        }
        private bool ReloadConfiguration()
        {
            try
            {
                _configurationRoot.Reload();
            }
            catch
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
                object? propertyObject = property.Value.ValueKind switch
                {
                    JsonValueKind.Object => ConvertToDictionary(property.Value),
                    JsonValueKind.Array => ConvertToList(property.Value),
                    JsonValueKind.String => property.Value.GetString(),
                    JsonValueKind.Number => property.Value.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null,
                    _ => property.Value.ToString() ?? string.Empty
                };
                if (propertyObject is null)
                    continue;
                dict[property.Name] = propertyObject;
            }

            return dict;
        }

        private List<object> ConvertToList(JsonElement element)
        {
            var list = new List<object>();

            foreach (var item in element.EnumerateArray())
            {
                object? itemObject = item.ValueKind switch
                {
                    JsonValueKind.Object => ConvertToDictionary(item),
                    JsonValueKind.Array => ConvertToList(item),
                    JsonValueKind.String => item.GetString(),
                    JsonValueKind.Number => item.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null,
                    _ => item.ToString() ?? string.Empty
                };
                if (itemObject is null)
                    continue;
                list.Add(itemObject);
            }
            return list;
        }

        private void SetNestedValue(Dictionary<string, object> dict, string path, object value)
        {
            var parts = path.Split(':');
            var current = dict;

            for (int i = 0; i < parts.Length - 1; i++)
            {
                if (!current?.ContainsKey(parts[i]) ?? false)
                {
                    current?[parts[i]] = new Dictionary<string, object>();
                }
                current = current?[parts[i]] as Dictionary<string, object>;
            }
            if (current is null)
                return;
            current[parts[^1]] = value;
        }
    }
}
