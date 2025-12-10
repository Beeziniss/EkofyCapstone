using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using AutoMapper;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Tests.Helpers;

namespace EkofyApp.Tests.Helpers;

/// <summary>
/// Base test class providing common setup and utilities for all service tests
/// </summary>
public abstract class BaseServiceTest
{
    protected readonly Mock<IUnitOfWork> MockUnitOfWork;
    protected readonly Mock<IHttpContextAccessor> MockHttpContextAccessor;
    protected readonly Mock<IRedisCacheService> MockRedisCacheService;
    protected readonly DefaultHttpContext MockHttpContext;
    protected readonly Mock<IMapper> MockMapper;
    protected readonly IMapper _mapper;

    protected BaseServiceTest()
    {
        MockUnitOfWork = new Mock<IUnitOfWork>();
        MockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        MockRedisCacheService = new Mock<IRedisCacheService>();
        MockHttpContext = new DefaultHttpContext();
        MockMapper = new Mock<IMapper>();
        _mapper = MockMapper.Object;

        MockHttpContextAccessor.Setup(x => x.HttpContext).Returns(MockHttpContext);
        
        // Set up default user
        MockHttpContext.User = CreateTestUser(Guid.NewGuid().ToString());
    }

    protected Mock<IMongoCollection<T>> SetupMockCollection<T>(List<T>? data = null) where T : class
    {
        var mockCollection = MockMongoCollectionHelper.Create(data ?? new List<T>());
        MockUnitOfWork.Setup(x => x.GetCollection<T>()).Returns(mockCollection.Object);
        return mockCollection;
    }

    protected void SetupSuccessfulTransaction()
    {
        MockUnitOfWork.Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<IClientSessionHandle, Task>>()))
                     .Returns((Func<IClientSessionHandle, Task> operation) => 
                     {
                         var mockSession = new Mock<IClientSessionHandle>();
                         return operation(mockSession.Object);
                     });
    }

    protected void VerifyTransactionExecuted(Times? times = null)
    {
        var expectedTimes = times ?? Times.Once();
        MockUnitOfWork.Verify(x => x.ExecuteInTransactionAsync(It.IsAny<Func<IClientSessionHandle, Task>>()), 
                             expectedTimes);
    }

    protected static ClaimsPrincipal CreateTestUser(string userId, string role = "Listener")
    {
        var claims = new List<Claim>
        {
            new("listenerId", userId), // Add listenerId claim that the services expect
            new("userId", userId), // Add userId claim that the services expect
            new(ClaimTypes.Role, role),
            new("avatarImage", "https://image.com"),
        };

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        return new ClaimsPrincipal(identity);
    }

    protected void SetupMockLogger<T>(Mock<ILogger<T>> mockLogger)
    {
        mockLogger.Setup(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)));
    }

    protected void SetupCollectionWithProjection<TDocument, TProjection>(
        List<TDocument> documents,
        List<TProjection> projections) where TDocument : class
    {
        var mockCollection = SetupMockCollection(documents);

        var mockProjectedFindFluent = new Mock<IFindFluent<TDocument, TProjection>>();
        mockProjectedFindFluent.Setup(x => x.ToListAsync(It.IsAny<CancellationToken>()))
                              .ReturnsAsync(projections);
        mockProjectedFindFluent.Setup(x => x.FirstOrDefaultAsync(It.IsAny<CancellationToken>()))
                              .ReturnsAsync(projections.FirstOrDefault());
        mockProjectedFindFluent.Setup(x => x.AnyAsync(It.IsAny<CancellationToken>()))
                              .ReturnsAsync(projections.Any());
        mockProjectedFindFluent.Setup(x => x.Limit(It.IsAny<int?>()))
                              .Returns(mockProjectedFindFluent.Object);
        mockProjectedFindFluent.Setup(x => x.Skip(It.IsAny<int?>()))
                              .Returns(mockProjectedFindFluent.Object);
        mockProjectedFindFluent.Setup(x => x.Sort(It.IsAny<SortDefinition<TDocument>>()))
                              .Returns(mockProjectedFindFluent.Object);

        var findFluent = mockCollection.Object.Find(FilterDefinition<TDocument>.Empty);
        ((Mock<IFindFluent<TDocument, TDocument>>)Mock.Get(findFluent))
            .Setup(x => x.Project<TProjection>(It.IsAny<ProjectionDefinition<TDocument, TProjection>>()))
            .Returns(mockProjectedFindFluent.Object);
    }
}