using System.Threading;
using System.Threading.Tasks;
using JamCreator.Controllers;
using JamCreator.Shared.Interfaces;
using JamCreator.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

public class ParticipantControllerTests
{
    [Fact]
    public async Task Delete_ExistingId_ReturnsNoContent()
    {
        var repoMock = new Mock<IRepository<SessionParticipant, int>>();
        repoMock
            .Setup(r => r.DeleteByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var controller = new ParticipantController(repoMock.Object);

        var result = await controller.Delete(5, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_NonExistingId_ReturnsNotFound()
    {
        var repoMock = new Mock<IRepository<SessionParticipant, int>>();
        repoMock
            .Setup(r => r.DeleteByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var controller = new ParticipantController(repoMock.Object);

        var result = await controller.Delete(5, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }
}
