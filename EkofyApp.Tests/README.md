# EkofyApp Unit Tests

This test project contains comprehensive unit tests for all major services in the EkofyApp application.

## Test Coverage

The following services are covered with unit tests:

### Core Services
- **TrackService** - Music track management, favorites, streaming, search functionality
- **PlaylistService** - Playlist creation, management, and recommendations
- **UserService** - User management, following/followers, restrictions, banning/unbanning
- **AuthenticationService** - Login, registration, OTP verification, password management
- **ArtistService** - Artist profiles, registration approval, revenue calculation
- **ListenerService** - Listener profiles and profile updates

### Subscription Services
- **SubscriptionService** - Subscription management, activation, metadata updates
- **SubscriptionPlanService** - Subscription plan queries and management

### Category Service
- **CategoryService** - Category management, mood detection from audio features

## Test Infrastructure

### Base Classes
- `BaseServiceTest` - Base class providing common mock setup and utilities
- `MockMongoCollectionHelper` - Helper for creating mock MongoDB collections
- `TestDataHelper` - Factory methods for creating test entities

### Test Utilities
- Comprehensive mocking of MongoDB operations
- HTTP context and user claim setup
- Redis cache service mocking
- Transaction handling mocks

## Running Tests

```bash
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "ClassName=TrackServiceTests"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Test Structure

Each service test class follows this pattern:

1. **Setup** - Initialize service with mocked dependencies
2. **Arrange** - Setup test data and mock behaviors  
3. **Act** - Execute the method under test
4. **Assert** - Verify expected outcomes and interactions

## Key Testing Scenarios

### Authentication Tests
- User registration (Listener/Artist)
- Login with various user roles
- OTP verification flow
- Password change and reset
- Google authentication integration

### Track Management Tests
- Track creation and metadata updates
- Favorite/unfavorite functionality
- Stream count tracking
- Search and filtering
- Audio feature processing

### User Relationship Tests
- Follow/unfollow functionality
- Follower/following queries
- User restrictions and banning
- Permission checks

### Subscription Tests
- Subscription creation and activation
- Plan management
- Metadata updates
- State transitions

### Playlist Tests
- Playlist CRUD operations
- Track addition/removal
- Favorite playlists
- Daily recommendations

### Category Tests
- Category management
- Mood detection algorithms
- Audio feature analysis
- Alternative description generation

## Mock Strategies

- **MongoDB Collections** - Custom mock implementation with LINQ support
- **HTTP Context** - Mocked with configurable user claims
- **Redis Cache** - Mocked with realistic cache behaviors
- **External Services** - Mocked to isolate unit under test
- **Transactions** - Mocked to test transactional operations

## Best Practices

1. **Isolation** - Each test is independent and doesn't affect others
2. **Comprehensive Coverage** - Tests cover happy paths, error cases, and edge cases
3. **Clear Naming** - Test method names clearly describe the scenario
4. **Realistic Data** - Test data resembles real application data
5. **Proper Mocking** - Mocks are configured to behave like real dependencies

## Dependencies

- **xUnit** - Testing framework
- **Moq** - Mocking framework
- **FluentAssertions** - Fluent assertion library
- **MongoDB.Driver** - MongoDB operations
- **ASP.NET Core** - HTTP context and authentication

## Notes

- Tests use in-memory collections for MongoDB operations
- Authentication tests include comprehensive flow testing
- Complex business logic (like mood detection) is thoroughly tested
- Error scenarios and exception handling are well covered