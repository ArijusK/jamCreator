using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using JamCreator.Data;
using JamCreator.Shared.Models;

namespace JamCreator.Server.IntegrationTests
{
    public class ChatHubIntegrationTests
    {
        private static AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public async Task SendMessage_ValidInput_SavesMessageInDatabase()
        {
            using var db = CreateContext();

            var mockClients = new Mock<IHubCallerClients>();
            var mockOthers = new Mock<IClientProxy>();
            mockClients.Setup(c => c.Others).Returns(mockOthers.Object);

            var hub = new ChatHub(db)
            {
                Clients = mockClients.Object
            };

            await hub.SendMessage("Alice", "Hello", "😎");

            var saved = await db.ChatMessages.SingleAsync();
            Assert.Equal("Alice", saved.User);
            Assert.Equal("Hello", saved.Text);
            Assert.Equal("😎", saved.Avatar);
        }

        [Fact]
        public async Task SendMessage_ValidInput_CallsSignalRWithCorrectPayload()
        {
            using var db = CreateContext();

            var mockClients = new Mock<IHubCallerClients>();
            var mockOthers = new Mock<IClientProxy>();
            mockClients.Setup(c => c.Others).Returns(mockOthers.Object);

            var hub = new ChatHub(db)
            {
                Clients = mockClients.Object
            };

            await hub.SendMessage("Bob", "Yo", null);

            mockOthers.Verify(m =>
                m.SendCoreAsync(
                    "ReceiveMessage",
                    It.Is<object[]>(args =>
                        args.Length >= 4 &&
                        (string)args[0] == "Bob" &&
                        (string)args[1] == "Yo" &&
                        args[2] == null &&
                        args[3] is DateTime
                    ),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

    }
}
