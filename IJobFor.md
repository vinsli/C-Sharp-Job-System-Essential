# IJobFor

上一节我们了解了什么是C# Job System，并且使用了**IJob**接口完成了一个简单的AddJob。这一节让我们一起来看一下另外一个有意思的接口**IJobFor**。

**IJobFor**是**IJobParalleFor**的继任者，它包含了后者的所有功能，并且提供了更好的灵活性，因此我们应该尽量使用**IJobFor**而不是**IJobParalleFor**。

让我们先来对比一下**IJob**和**IJobFor**两接口有什么不同：
>注：例子来自于[Unity官方文档](https://docs.unity3d.com/ScriptReference/Unity.Jobs.IJobFor.html)

```C#
public interface IJobFor
{
    void Execute(int index);
}

public interface IJob
{
    void Execute();
}
```

相对于**IJob**，**IJobFor**接口的Execute()方法多了一个index参数，通过这个参数我们可以访问Job中的NativeContainer容器，对容器中的元素进行相对独立的操作。

除此之外，IJobFor还在任务的调度上给我们提供了更大的灵活性。我们可以用下面三种方式来schedule我们的Job：

```C#
public void Update()
{
    ...
    var position = new NativeArray<Vector3>(500, Allocator.Persistent);

    var job = new VelocityJob();
    //run on main thread
    job.Run(position.Length);
    //run on a single worker thread
    job.Schedule(position.Length, dependency);
    //run on parallel worker threads
    job.ScheduleParallel(position.Length, 64, dependency);
    ...
}
```

以上三种方式都需要传入一个arrayLength参数，通过这个参数我们可以控制Execute()方法执行的次数。
实际上我们传入的arrayLength不一定就是数组的长度，它可以是小于数组长度的任意数值，这也给我们Job执行带来了一定的灵活性。

总的来说，通过选择Run, Schedule, ScheduleParallel让我们可以根据任务的特点或使用场景来灵活的进行任务调度。

接下来让我们来看一下IJobFor的具体实现：

```C#
struct VelocityJob : IJobFor
{
    [ReadOnly]
    public NativeArray<Vector3> velocity;
    public NativeArray<Vector3> position;
    public float deltaTime;

    public void Execute(int i)
    {
        position[i] = position[i] + velocity[i] * deltaTime;
    }
}
```

首先能注意到的是Execute()方法中，我们通过传入的**i**来访问velocity和position数组，这里就产生了一个问题，如果我们使用**i+1**会发生什么呢？如果你试一下就会得到跟下面类似的一个Exception.

>IndexOutOfRangeException: Index 64 is out of restricted IJobParallelFor range [0...63] in ReadWriteBuffer.

这其实是C# Job System的Safety system在起作用。他会最大限度保证大家在书写多线程代码时的安全性。

另外一个值得注意的地方就是[ReadOnly]属性。当我们把velocity标记为ReadOnly时，我们可以在多个并行的Job中读取velocity数组的内容而不触发safety system。因此我们应该尽量将Job中只读的数据标记成ReadOnly来最大化我们的性能。

好了，以上就是IJobFor的基本用法了，很简单不是么😉，下一节让我们来用IJobFor做一点不一样的东西。
