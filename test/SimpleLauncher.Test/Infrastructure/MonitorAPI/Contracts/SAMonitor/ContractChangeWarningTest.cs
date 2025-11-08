using SimpleLauncher.Infrastructure.MonitorAPI.Contracts.SAMonitor;
using System.ComponentModel;
namespace SimpleLauncher.Test.Infrastructure.MonitorAPI.Contracts.SAMonitor
{
    /// <summary>
    /// If this test failed - the record's backward compatibility has been broken. 
    /// Please make shure it is what you want to do and change this test so that
    /// it will pass with your new changes
    /// </summary>
    /// <remarks>
    /// This test verifies critical backward compatibility behavior of record types.
    /// Breaking changes to record semantics can affect:
    /// - Serialization/deserialization across service versions
    /// - Data persistence and migration scenarios
    /// - Client-server compatibility in distributed systems
    /// - Hash-based collections and dictionaries
    /// 
    /// Before modifying this test, ensure all stakeholders are aware of the compatibility impact.
    /// Only modify records if web service endpoint's contracts was changed!
    /// </remarks>
    [Trait("Category", "BackwardCompatibility")]
    [Trait("Priority", "Critical")]
    [Trait("Type", "Record")]
    [DisplayName("Record Backward Compatibility - CRITICAL")]
    [Description("Tests that record semantics remain compatible with existing systems")]
    public class ContractChangeWarningTest
    {
        private const string IpAddress01 = "127.0.0.1";
        private const string IpAddress02 = "server.samp-dogfighters.ru:7777";
        /// <summary>
        /// Test for backward compatability of the record GetServerByIpRequest
        /// </summary>
        /// <remarks>
        /// Only modify records if remote service contracts was changed!
        /// </remarks>
        [Fact]
        public void GetServerByIpRequestUnitTest_WarnsIfCompatabilityBroken()
        {
            var request1 = new GetServerByIpRequest(IpAddress01);
            var request2 = new GetServerByIpRequest(IpAddress02);
            var request3 = new GetServerByIpRequest(IpAddress01);
            Assert.Equal(request1, request3);
            Assert.NotEqual(request1, request2);
            Assert.True(request1 == request3);
        }
        /// <summary>
        /// Test for backward compatability of the record GetServerByIpResponse
        /// </summary>
        /// <remarks>
        /// Only modify records if remote service contracts was changed!
        /// </remarks>
        [Fact]
        public void GetServerByIpResponseUnitTest_WarnsIfCompatabilityBroken()
        {
            const int id = 0;
            const bool success = true;
            var lastUpdated = DateTime.UtcNow;
            var worldTime = DateTime.Now;
            const uint online = 20;
            const uint max = 100;
            const bool isOmp = true;
            const bool isLagcomp = true;
            const string name = "Test name";
            const string mode = "Test mode";
            const string ipAddress = IpAddress01;
            const string mapName = "Test mapname";
            const string webSite = "https://yandex.ru";
            const string version = "2.0.2.5";
            const string language = "Russian";
            const string cac = "Yes";
            const bool requiresPassword = true;
            const int shuffledOrder = 0;
            const int weather = 17;
            const bool sponsor = false;
            var response1 = new GetServerByIpResponse(id,
                success,
                lastUpdated,
                worldTime,
                online,
                max,
                isOmp,
                isLagcomp,
                name,
                mode,
                ipAddress,
                mapName,
                webSite,
                version,
                language,
                cac,
                requiresPassword,
                shuffledOrder,
                weather,
                sponsor);
            var response2 = new GetServerByIpResponse(id,
                !success,
                lastUpdated,
                worldTime,
                online,
                max,
                isOmp,
                isLagcomp,
                name + name,
                mode,
                ipAddress,
                mapName,
                webSite,
                version,
                language,
                cac,
                requiresPassword,
                shuffledOrder,
                weather,
                sponsor);
            var response3 = new GetServerByIpResponse(id,
                success,
                lastUpdated,
                worldTime,
                online,
                max,
                isOmp,
                isLagcomp,
                name,
                mode,
                ipAddress,
                mapName,
                webSite,
                version,
                language,
                cac,
                requiresPassword,
                shuffledOrder,
                weather,
                sponsor);
            Assert.Equal(response1, response3);
            Assert.NotEqual(response1, response2);
            Assert.True(response1 ==  response3);
        }
        /// <summary>
        /// Test for backward compatability of the record GetServerPlayersRequest
        /// </summary>
        /// <remarks>
        /// Only modify records if remote service contracts was changed!
        /// </remarks>
        [Fact]
        public void GetServerPlayersRequestUnitTest_WarnsIfCompatabilityBroken()
        {
            var ipPort = IpAddress01.Split(':');
            var request1 = new GetServerPlayersRequest(ipPort[0], ipPort[1]);
            var request2 = new GetServerPlayersRequest(ipPort[0], ipPort[1]);
            var request3 = new GetServerPlayersRequest(ipPort[0], ipPort[1]);
            Assert.Equal(request1, request3);
            Assert.NotEqual(request1, request2);
            Assert.True(request1 == request3);
        }
        /// <summary>
        /// Test for backward compatability of the record GetServerPlayersResponse
        /// </summary>
        /// <remarks>
        /// Only modify records if remote service contracts was changed!
        /// </remarks>
        [Fact]
        public void GetServerPlayersResponseUnitTest_WarnsIfCompatabilityBroken()
        {
            const int id = 0;
            const string name = "Test";
            const int ping = 125;
            const int score = 100500;

            var response1 = new GetServerPlayersResponse(
                new List<SimpleLauncher.Infrastructure.MonitorAPI.Models.SAMonitorPlayer>()
                {
                    new SimpleLauncher.Infrastructure.MonitorAPI.Models.SAMonitorPlayer()
                    {
                        Id = id,
                        Ping = ping,
                        Name = name,
                        Score = score
                    }
                });
            var response2 = new GetServerPlayersResponse(
                new List<SimpleLauncher.Infrastructure.MonitorAPI.Models.SAMonitorPlayer>()
                {
                    new SimpleLauncher.Infrastructure.MonitorAPI.Models.SAMonitorPlayer()
                    {
                        Id = id,
                        Ping = ping,
                        Name = name + name,
                        Score = score
                    }
                });
            var response3 = new GetServerPlayersResponse(
                new List<SimpleLauncher.Infrastructure.MonitorAPI.Models.SAMonitorPlayer>()
                {
                    new SimpleLauncher.Infrastructure.MonitorAPI.Models.SAMonitorPlayer()
                    {
                        Id = id,
                        Ping = ping,
                        Name = name,
                        Score = score
                    }
                });
            Assert.Equal(response1, response3);
            Assert.NotEqual(response1, response2);
            Assert.True(response1 == response3);
        }
        /// <summary>
        /// Test for backward compatability of the record GetServersRequest
        /// </summary>
        /// <remarks>
        /// Only modify records if remote service contracts was changed!
        /// </remarks>
        [Fact]
        public void GetServersRequestUnitTest_WarnsIfCompatabilityBroken()
        {
            var request1 = new GetServersRequest();
            Assert.Equal(request1, new GetServersRequest());
        }
        /// <summary>
        /// Test for backward compatability of the record GetServersResponse
        /// </summary>
        /// <remarks>
        /// Only modify records if remote service contracts was changed!
        /// </remarks>
        [Fact]
        public void GetServersResponseUnitTest_WarnsIfCompatabilityBroken()
        {
            const int id = 0;
            const bool success = true;
            var lastUpdated = DateTime.UtcNow;
            var worldTime = DateTime.Now;
            const uint online = 20;
            const uint max = 100;
            const bool isOmp = true;
            const bool isLagcomp = true;
            const string name = "Test name";
            const string mode = "Test mode";
            const string ipAddress = IpAddress01;
            const string mapName = "Test mapname";
            const string webSite = "https://yandex.ru";
            const string version = "2.0.2.5";
            const string language = "Russian";
            const string cac = "Yes";
            const bool requiresPassword = true;
            const int shuffledOrder = 0;
            const int weather = 17;
            const bool sponsor = false;
            var response1 = new GetServersResponse(id,
                success,
                lastUpdated,
                worldTime,
                online,
                max,
                isOmp,
                isLagcomp,
                name,
                mode,
                ipAddress,
                mapName,
                webSite,
                version,
                language,
                cac,
                requiresPassword,
                shuffledOrder,
                weather,
                sponsor);
            var response2 = new GetServersResponse(id,
                success,
                lastUpdated,
                worldTime,
                online,
                max,
                isOmp,
                isLagcomp,
                name + name,
                mode,
                ipAddress,
                mapName,
                webSite,
                version,
                language,
                cac,
                requiresPassword,
                shuffledOrder,
                weather,
                sponsor);
            var response3 = new GetServersResponse(id,
                success,
                lastUpdated,
                worldTime,
                online,
                max,
                isOmp,
                isLagcomp,
                name,
                mode,
                ipAddress,
                mapName,
                webSite,
                version,
                language,
                cac,
                requiresPassword,
                shuffledOrder,
                weather,
                sponsor);
            Assert.Equal(response1, response3);
            Assert.NotEqual(response1, response2);
            Assert.True(response1 == response3);
        }
    }
}
