using System.Runtime.CompilerServices;
using Blazor.WebAssembly.DynamicCulture.Internals;

namespace Blazor.WebAssembly.DynamicCulture.Tests;

public class WeakRefCollectionTests
{
    [Fact]
    public void ForEach_VisitsDistinctInstancesOnlyOnce()
    {
        var collection = new WeakRefCollection<object>();
        var first = new object();
        var second = new object();
        collection.Add(first);
        collection.Add(first);
        collection.Add(second);
        var visited = new List<object>();

        collection.ForEach(visited.Add);

        Assert.Equal([first, second], visited);
        GC.KeepAlive(first);
        GC.KeepAlive(second);
    }

    [Fact]
    public void ForEach_EmptyCollectionDoesNotInvokeCallback()
    {
        new WeakRefCollection<object>().ForEach(_ => Assert.Fail("An empty collection has no targets."));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CollectedTargets_AreSkippedAndDoNotPreventAddingLiveTargets(bool addBeforeSweep)
    {
        var collection = new WeakRefCollection<object>();
        var weak = AddCollectibleTarget(collection);
        var alive = new object();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        Assert.False(weak.IsAlive);

        if (addBeforeSweep)
        {
            collection.Add(alive);
        }

        var visited = new List<object>();
        collection.ForEach(visited.Add);

        Assert.Equal(addBeforeSweep ? [alive] : Array.Empty<object>(), visited);
        GC.KeepAlive(alive);
    }

    [Fact]
    public void ForEach_CallbackCanRegisterAnotherTargetForTheNextNotification()
    {
        var collection = new WeakRefCollection<object>();
        var first = new object();
        var addedDuringNotification = new object();
        collection.Add(first);
        var visited = new List<object>();

        collection.ForEach(item =>
        {
            visited.Add(item);
            collection.Add(addedDuringNotification);
        });

        Assert.Same(first, Assert.Single(visited));
        visited.Clear();
        collection.ForEach(visited.Add);
        Assert.Equal([first, addedDuringNotification], visited);
        GC.KeepAlive(first);
        GC.KeepAlive(addedDuringNotification);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference AddCollectibleTarget(WeakRefCollection<object> collection)
    {
        var target = new object();
        collection.Add(target);
        return new WeakReference(target);
    }
}
