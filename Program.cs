using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Distributed;

// Build configuration
IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    //.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true)
    .Build();

// Set up dependency injection
var services = new ServiceCollection();
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = configuration.GetSection("RedisCacheOptions:ConnectionString").Value;
    options.InstanceName = "TestInstance:"; // Optional: Prefix for cache keys
});

// Add IDistributedCache to services
var serviceProvider = services.BuildServiceProvider();

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
    Console.WriteLine($"Cache set: {key} = {value}");

    // Retrieve the cache entry  
    string? cachedValue = await cache.GetStringAsync(key);
    Console.WriteLine($"Cache get: {key} = {cachedValue ?? "Not found"}");

    // Optionally, remove the cache entry
    await cache.RemoveAsync(key);
    Console.WriteLine($"Cache removed: {key}");

    // Verify removal
    cachedValue = await cache.GetStringAsync(key);
    Console.WriteLine($"Cache get after removal: {key} = {cachedValue ?? "Not found"}");
}
catch (Exception ex)
{
    Console.WriteLine($"Error interacting with Redis cache: {ex.Message}");
}