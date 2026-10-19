global using Xunit;

// Disable parallel test execution across classes so they don't reset the shared test database concurrently
[assembly: CollectionBehavior(DisableTestParallelization = true)]