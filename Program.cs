using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

// Determine environment (defaulting to Development)
string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

// Build configuration
IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

// Set up dependency injection
var services = new ServiceCollection();

// Add logging first
services.AddLogging(builder => 
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = configuration.GetSection("RedisCacheOptions:ConnectionString").Value;
    options.InstanceName = "TestInstance:"; // Optional: Prefix for cache keys
});

// Build service provider AFTER all services are registered
var serviceProvider = services.BuildServiceProvider();

// Get the logger AFTER the provider is built
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

logger.LogInformation("Begin: Redis cache demo starting");

// Get IDistributedCache instance
var cache = serviceProvider.GetRequiredService<IDistributedCache>();

// Test Redis cache
try
{
    // Set a cache entry
    string key = "testKey";
    string value = "Hello, Redis!";
    await cache.SetStringAsync(key, value, new DistributedCacheEntryOptions
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    });
    logger.LogInformation("Cache set: {Key} = {Value}", key, value);

    // Retrieve the cache entry  
    string? cachedValue = await cache.GetStringAsync(key);
    logger.LogInformation("Cache get: {Key} = {Value}", key, cachedValue ?? "Not found");

    // Optionally, remove the cache entry
    await cache.RemoveAsync(key);
    logger.LogInformation("Cache removed: {Key}", key);

    // Verify removal
    cachedValue = await cache.GetStringAsync(key);
    logger.LogInformation("Cache get after removal: {Key} = {Value}", key, cachedValue ?? "Not found");
}
catch (Exception ex)
{
    logger.LogError(ex, "Error interacting with Redis cache");
}

logger.LogInformation("End: Redis cache demo completed");