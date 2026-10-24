using Microsoft.Extensions.DependencyInjection;

namespace InvoiceFlow.LearningTests;

public class DependencyInjectionLifetimeTests
{
    private sealed class Counter;
    private sealed class NeedsCounter(Counter counter) { public Counter Counter { get; } = counter; }

    [Fact]
    public void Scoped_SameWithinScope_DifferentAcrossScopes()
    {
        var services = new ServiceCollection();
        services.AddScoped<Counter>();
        using ServiceProvider provider = services.BuildServiceProvider();

        using IServiceScope scopeA = provider.CreateScope();
        using IServiceScope scopeB = provider.CreateScope();

        Counter a1 = scopeA.ServiceProvider.GetRequiredService<Counter>();
        Counter a2 = scopeA.ServiceProvider.GetRequiredService<Counter>();
        Counter b1 = scopeB.ServiceProvider.GetRequiredService<Counter>();

        Assert.Same(a1, a2);
        Assert.NotSame(a1, b1);
    }

    [Fact]
    public void Transient_NewInstanceEveryTime()
    {
        var services = new ServiceCollection();
        services.AddTransient<Counter>();
        using ServiceProvider provider = services.BuildServiceProvider();

        using IServiceScope scopeA = provider.CreateScope();

        Counter a1 = scopeA.ServiceProvider.GetRequiredService<Counter>();
        Counter a2 = scopeA.ServiceProvider.GetRequiredService<Counter>();

        Assert.NotSame(a1, a2);
    }

    [Fact]
    public void Singleton_SameInstanceAcrossScopes()
    {
        var services = new ServiceCollection();
        services.AddSingleton<Counter>();
        using ServiceProvider provider = services.BuildServiceProvider();

        using IServiceScope scopeA = provider.CreateScope();
        using IServiceScope scopeB = provider.CreateScope();
        using IServiceScope scopeC = provider.CreateScope();

        Counter a1 = scopeA.ServiceProvider.GetRequiredService<Counter>();
        Counter b1 = scopeB.ServiceProvider.GetRequiredService<Counter>();
        Counter c1 = scopeC.ServiceProvider.GetRequiredService<Counter>();

        Assert.Same(a1, b1);
        Assert.Same(b1, c1);
    }

    [Fact]
    public void CaptiveDependency_IsRejected()
    {
        var services = new ServiceCollection();
        services.AddSingleton<NeedsCounter>();
        services.AddScoped<Counter>();

        var ex = Assert.Throws<AggregateException>(() => services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true }));
        Assert.Contains("Cannot consume scoped service", ex.InnerException!.Message);
    }

    [Fact]
    public void Transient_InsideSingleton_LivesAsLongAsTheSingleton()
    {
        var services = new ServiceCollection();
        services.AddTransient<Counter>();
        services.AddSingleton<NeedsCounter>();
        using ServiceProvider provider = services.BuildServiceProvider();

        NeedsCounter first = provider.GetRequiredService<NeedsCounter>();
        NeedsCounter second = provider.GetRequiredService<NeedsCounter>();

        Assert.Same(first.Counter, second.Counter);
    }
}