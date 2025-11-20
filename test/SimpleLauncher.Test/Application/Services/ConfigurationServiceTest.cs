using System.Text.Json;
using Microsoft.Extensions.Configuration;
using SimpleLauncher.Application.Services;
using SimpleLauncher.Domain.Models;

namespace SimpleLauncher.Test.Application.Services
{
    public class ConfigurationServiceTest
    {
        private const string TestConfigFileName = "test_settings.json";

        private IConfigurationRoot GetConfigurationRoot(string filePath)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(filePath, optional: false, reloadOnChange: false);
            return builder.Build() as IConfigurationRoot;
        }

        private void WriteConfig(string json)
        {
            File.WriteAllText(TestConfigFileName, json);
        }

        private void DeleteConfig()
        {
            if (File.Exists(TestConfigFileName))
                File.Delete(TestConfigFileName);
        }

        [Fact]
        public void EditGameClient_UpdatesExistingClient_ByName()
        {
            // Arrange
            WriteConfig(@"{
                ""ClientList"": {
                    ""Main"": { ""Version"": ""0.3.7-R5"", ""Path"": ""D:\\Path\\gta_sa.exe"" }
                }
            }");
            var configRoot = GetConfigurationRoot(TestConfigFileName);
            var service = new ConfigurationService(configRoot, TestConfigFileName);
            var client = new GameClient("Main", "0.3.7-R6", "D:\\Path\\gta_sa.exe", "client.exe", "game.exe");

            // Act
            var result = service.EditGameClient("Main", client);

            // Assert
            Assert.True(result);
            var json = File.ReadAllText(TestConfigFileName);
            var doc = JsonDocument.Parse(json);
            var version = doc
                .RootElement
                .GetProperty("ClientList")
                .GetProperty("Main")
                .GetProperty("Version")
                .GetString();
            Assert.Equal("0.3.7-R6", version);

            DeleteConfig();
        }

        [Fact]
        public void EditGameClient_UpdatesExistingClient_ByPath()
        {
            // Arrange
            WriteConfig(@"{
                ""ClientList"": {
                    ""Samp Wars"": { ""Version"": ""0.3DL"", ""Path"": ""C:\\Path\\gta_sa.exe"" }
                }
            }");
            var configRoot = GetConfigurationRoot(TestConfigFileName);
            var service = new ConfigurationService(configRoot, TestConfigFileName);
            var client = new GameClient("Wars", "0.3DL-R2", "C:\\Path\\gta_sa.exe", "client.exe", "game.exe");

            // Act
            var result = service.EditGameClient("LegacyName", client);

            // Assert
            Assert.True(result);
            var json = File.ReadAllText(TestConfigFileName);
            var doc = JsonDocument.Parse(json);
            var version = doc.RootElement.GetProperty("ClientList").GetProperty("Samp Wars").GetProperty("Version").GetString();
            Assert.Equal("0.3DL-R2", version);

            DeleteConfig();
        }

        [Fact]
        public void EditGameClient_AddsNewClient_IfNotExists()
        {
            // Arrange
            WriteConfig(@"{
                ""ClientList"": {
                    ""Main"": { ""Version"": ""0.3.7-R5"", ""Path"": ""D:\\Path\\gta_sa.exe"" }
                }
            }");
            var configRoot = GetConfigurationRoot(TestConfigFileName);
            var service = new ConfigurationService(configRoot, TestConfigFileName);
            var client = new GameClient("NewClient", "0.3.7-R7", "E:\\Games\\gta_sa.exe", "client.exe", "game.exe");

            // Act
            var result = service.EditGameClient("LegacyName", client);

            // Assert
            Assert.True(result);
            var json = File.ReadAllText(TestConfigFileName);
            var doc = JsonDocument.Parse(json);
            var newClient = doc.RootElement.GetProperty("ClientList").GetProperty("NewClient");
            Assert.Equal("0.3.7-R7", newClient.GetProperty("Version").GetString());
            Assert.Equal("E:\\Games\\gta_sa.exe", newClient.GetProperty("Path").GetString());

            DeleteConfig();
        }
    }
}