# Pointers & InterLocked

这一节我们来改造一下最开始的[CounterJob](What_is_C_Sharp_JobSystem.md)利用IJobFor接口开实现并行求和。

我们先来一个Naive版本，来一起思考一下这个Job中的问题：

```C#
public struct NaiveParallelCounterJob : IJobFor
{
    [ReadOnly] public NativeArray<int> data;
    public int sum;

    public void Execute(int index)
    {
        sum += data[index];
    }
}
```

这个实现中我们只是简单的将``IJob``接口转换成了``IJobFor``接口。第一个非常明显的问题就是``sum``不是以引用的形式传进来的而是直接以值传递的形式进行了赋值，这会导致任务完成之后无法获取正确的``sum``值。这个问题在[什么是C# Job System？](What_is_C_Sharp_JobSystem.md)章节提到过。所幸的的是Unity已经为我们提供了``NativeReference<T>``这个很方便的封装类，它允许我们以引用的方式来传递单个值。
我们一起来修正这个问题：

```C#
public struct NaiveParallelCounterJob : IJobFor
{
    [ReadOnly] public NativeArray<int> data;
    public NativeReference<int> naiveSum;

    public void Execute(int index)
    {
        naiveSum.Value += data[index];
    }
}
```

我们解决了传引用的问题，接下来大家思考一下，``IJobFor``接口中的``Execute()``方法可能会运行在不同的Worker线程中，在这种情况下我们对``naiveSum``变量的访问就会存在竞争条件（race condition），如果我们直接执行这段代码，Unity的Safety System就会向我们报告一条错误：

``` Log
InvalidOperationException: NaiveParallelCounterJob.naiveSum is not declared [ReadOnly] in a IJobParallelFor job. The container does not support parallel writing. Please use a more suitable container type.
```

我们该如何修正这个错误呢？

很自然的，我们可以联想到上一节的线程本地存储（Thread Local Storage），我们将上一节TLS实现搬运到ParallelCounter中就得到了下面的代码：

```C#
public struct ThreadLocalParallelCounterJob : IJobFor
{
    //input
    [NativeDisableParallelForRestriction] public NativeArray<int> data;

    //output
    [NativeDisableParallelForRestriction] public NativeArray<int> sums;

    [NativeSetThreadIndex] private int m_ThreadIndex;

    public void Execute(int index)
    {
        sums[m_ThreadIndex] += data[index];
    }
}
```

不过这个Job中我们只得到了按照线程ID分组的sum值，我们还需要另外一个Job将这些sum组合成一个TotalSum。

```C#
public struct TotalSumJob : IJob
{
    [ReadOnly] public NativeArray<int> sums;

    public NativeReference<int> totalSum;

    public void Execute()
    {
        for (int i = 0; i < sums.Length; i++)
        {
            totalSum.Value += sums[i];
        }
    }
}
```

在Job调度代码中将这两个Job以依赖（dependency）的形式串联起来。

```C#
var threadLocalCounterJobHandle = threadLocalCounterJob.ScheduleParallel(m_Data.Length, 64, new JobHandle());
var totalSumJobHandle = totalSumJob.Schedule(threadLocalCounterJobHandle);

totalSumJobHandle.Complete();
```

经过一番折腾，我们得到了一个可以正常运行的ParallelCounter。但是大家有没有注意到，上面分享的例子，跟今天的标题根本就不相关啊🤣。好，下面我们就用指针和Interlocked类来实现一下这个ParallelCounter。

[Interlocked](https://docs.microsoft.com/en-us/dotnet/api/system.threading.interlocked?view=net-6.0)可以为我们在不同线程之间以原子操作的方式来修改变量，这样我们就不用担心竞争条件（race condition）的问题了。

为了在Job中使用指针，我们需要引入一个新的属性——``[NativeDisableUnsafePtrRestriction]``，有了这个属性，我们就可以解除在Job中不能使用指针的限制，大大扩展了我们书写Job的灵活性。

下面我们给出Interlocked版本的实现：

```C#
public unsafe struct InterlockedParallelCounterJob : IJobFor
{
    //input
    public NativeArray<int> data;

    //output
    [NativeDisableUnsafePtrRestriction] public int* sum;

    public void Execute(int index)
    {
        Interlocked.Add(ref UnsafeUtility.AsRef<int>(sum), data[index]);
    }
}
```

这里值得一提的是，``Interlocked.Add()``方法需要一个``ref``变量，我们需要将``sum``指针转换成``ref``变量，而``UnsafeUtility.AsRef()``正好就是我们需要的。[UnsafeUtility](https://docs.unity3d.com/ScriptReference/Unity.Collections.LowLevel.Unsafe.UnsafeUtility.html)类提供了很多大家书写native代码的封装，大家有需要可以优先找一下这里是否已经有了你需要的方法实现。
