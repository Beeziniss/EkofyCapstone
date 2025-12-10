using MongoDB.Driver;
using Moq;
using System.Linq.Expressions;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Tests.Helpers;

public static class MockMongoCollectionHelper
{
    public static Mock<IMongoCollection<T>> Create<T>(List<T> data) where T : class
    {
        var mockCollection = new Mock<IMongoCollection<T>>();

        // Create an in-memory queryable from the test data
        var queryable = data.AsQueryable();

        // Mock the collection to implement IQueryable<T> directly
        // This allows the collection itself to be treated as IQueryable when AsQueryable() is called
        mockCollection.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
        mockCollection.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
        mockCollection.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        mockCollection.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());

        // MOCK CURSOR that returns our test data
        var mockCursor = new Mock<IAsyncCursor<T>>();
        
        // For empty collections, Current should return empty enumerable
        // For non-empty collections, Current should return the data
        if (data.Any())
        {
            mockCursor.Setup(_ => _.Current).Returns(data);
            
            mockCursor
                .SetupSequence(_ => _.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);

            mockCursor
                .SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);
        }
        else
        {
            // For empty collections, Current should return empty enumerable and MoveNext should return false
            mockCursor.Setup(_ => _.Current).Returns(new List<T>());
            
            mockCursor
                .Setup(_ => _.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(false);

            mockCursor
                .Setup(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
        }

        // MOCK CountDocumentsAsync - improved to handle User email filters
        mockCollection
            .Setup(x => x.CountDocumentsAsync(
                It.IsAny<FilterDefinition<T>>(),
                It.IsAny<CountOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns<FilterDefinition<T>, CountOptions, CancellationToken>((filter, options, token) =>
            {
                var count = SimulateFilterCount(data, filter);
                return Task.FromResult((long)count);
            });

        // MOCK FindAsync - This is the method that Find() extension method actually calls
        mockCollection
            .Setup(x => x.FindAsync(
                It.IsAny<FilterDefinition<T>>(),
                It.IsAny<FindOptions<T, T>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);

        // The key insight: Find() extension method calls FindAsync under the hood
        // So we need to make sure our cursor properly simulates the data retrieval
        
        // MOCK InsertOneAsync
        mockCollection
            .Setup(x => x.InsertOneAsync(
                It.IsAny<T>(),
                It.IsAny<InsertOneOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // MOCK UpdateOneAsync
        var updateResult = Mock.Of<UpdateResult>(u =>
            u.MatchedCount == 1 &&
            u.ModifiedCount == 1
        );

        mockCollection
            .Setup(x => x.UpdateOneAsync(
                It.IsAny<FilterDefinition<T>>(),
                It.IsAny<UpdateDefinition<T>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(updateResult);

        // MOCK DeleteOneAsync
        var deleteResult = Mock.Of<DeleteResult>(d =>
            d.DeletedCount == 1
        );

        mockCollection
            .Setup(x => x.DeleteOneAsync(
                It.IsAny<FilterDefinition<T>>(),
                It.IsAny<DeleteOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(deleteResult);

        return mockCollection;
    }

    private static int SimulateFilterCount<T>(List<T> data, FilterDefinition<T> filter) where T : class
    {
        // Special handling for User entities and email filters
        if (typeof(T) == typeof(User))
        {
            var users = data.Cast<User>().ToList();
            
            // Try to extract the filter as a simple expression filter
            if (filter is ExpressionFilterDefinition<T> expressionFilter)
            {
                try
                {
                    // Compile and execute the expression against our test data
                    var compiledExpression = expressionFilter.Expression.Compile();
                    return data.Count(item => compiledExpression((T)item));
                }
                catch
                {
                    // If compilation fails, fall back to string-based matching
                }
            }
            
            // Fallback: Check if any user in the data has an email that might match
            // This is a simple heuristic for the common case of email equality filters
            var filterString = filter.ToString();
            
            // Look for patterns like {"Email": "example@example.com"} in the filter string
            foreach (var user in users)
            {
                if (!string.IsNullOrEmpty(user.Email) && filterString.Contains(user.Email))
                {
                    return 1; // Found a match
                }
            }
        }
        
        // For other types or when no match is found, return 0
        return 0;
    }

    private static void SetupMockFluentChain<T>(Mock<IAggregateFluent<T>> fluentMock) where T : class
    {
        // Mock returns self for method chaining
        fluentMock.Setup(x => x.Match(It.IsAny<FilterDefinition<T>>()))
                  .Returns(fluentMock.Object);
                  
        // For complex generic methods, just use DefaultValue.Mock behavior
        // This will automatically create new mocks as needed
        fluentMock.DefaultValue = DefaultValue.Mock;
        
        // NOTE: We cannot mock FirstOrDefaultAsync as it's an extension method
        // The service will need to be tested differently
    }
}