# 什么是C# Job System？
C# Job System是Unity从2018开始提供给用户的一个非常强大的功能，它允许用户以一种低成本的方式书写高效的多线程代码。
我们先通过一个Demo来一步一步揭开C# Job System的面纱：

首先我们先来定义一个Job：

```C#
public struct AddJob : IJob
{
    public float a;
    public float b;
    public NativeArray<float> result;

    public void Execute()
    {
        result[0] = a + b;
    }
}
```

我们最先能观察到的就是AddJob实现了**IJob**接口，IJob可以让我们调度（Schedule）一个在单一工作（worker）线程里执行的任务。
其次我们可以看到AddJob是一个**struct**。

我们再来看一下AddJob里的变量部分：

```C#
public float a;
public float b;
public NativeArray<float> result;
```

Job中的变量我们**仅**可以使用[blittable types](https://en.wikipedia.org/wiki/Blittable_types)或者Unity为我们提供的[NativeContainer](https://docs.unity3d.com/Manual/JobSystemNativeContainer.html)容器，比如引擎内置的NativeArray或者com.unity.collections package中提供的容器类。

>注：为什么只能使用blittable types？这是因为C# Job System使用了Unity内部的native job system，C# Job System会与native job system共享工作线程。为了达到这个目的，C#中的Job数据需要被拷贝到native层来运行计算代码，blittable types在这个拷贝过程中不需要做数据转换，因此blittable types在这里是必须的。不仅如此blittable types还有着其他的好处，我们会在后面的例子中看到。

让我们来总结一下声明一个Job的要点：

1. 创建一个实现了IJob接口的struct。
2. 在struct中声明blittable types或者NativeContainer的变量。
3. 在Execute()方法中实现Job的逻辑。

好，通过上面几步我们就成功创建了我们的***AddJob*** 😀。接下来我们来看一下如何调度（Schedule）一个Job以及如何获得Job执行后的结果：

```C#
var job = new AddJob
{
    a = 1,
    b = 2,
    result = result
};

var handle = job.Schedule();
handle.Complete();
Debug.Log($"result = {result[0]}");
```

调度（Schedule）一个Job是比较简单的，只需要调用``Schedule()``方法就可以了。这里比较有意思的是Complete()方法，在我们需要读取执行结果之前需要调用``Complete()``方法。但是``Complete()``不一定在``Schedule()``之后立即调用，也不一定在当前帧必须调用，也就是说一个Job本身不受``Update()``限制可以跨帧运行。当一个Job需要跨帧运行的时候，我们需要使用``IsCompleted``属性来判断Job是否执行完毕。

```C#
private void Update()
{
    if (handle.IsCompleted)
    {
        handle.Complete();
        Debug.Log($"result = {result[0]}");
    }
}
```

>注：即使``IsCompleted``返回true，也必须要调用``Complete()``方法。具体可以参考[C# Job System tips and troubleshooting](https://docs.unity3d.com/Manual/JobSystemTroubleshooting.html)

这样我们就实现了了一个最简单的Job，这里我给出完整的Demo代码，方便大家进一步理解上面介绍的内容：

```C#
public class AddJobBehaviour : MonoBehaviour
{
    public bool longRunningJob;
    private JobHandle handle;
    private NativeArray<float> result;

    public struct AddJob : IJob
    {
        public float a;
        public float b;
        public NativeArray<float> result;

        public void Execute()
        {
            result[0] = a + b;
        }
    }

    private void Start()
    {
        result = new NativeArray<float>(1, Allocator.Persistent);

        var job = new AddJob
        {
            a = 1,
            b = 2,
            result = result
        };

        handle = job.Schedule();

        if (!longRunningJob)
        {
            handle.Complete();
            Debug.Log($"result = {result[0]}");
        }
    }

    private void Update()
    {
        if (handle.IsCompleted)
        {
            handle.Complete();
            Debug.Log($"result = {result[0]}");
        }
    }

    private void OnDestroy()
    {
        if (result.IsCreated)
            result.Dispose();
    }
}
```

在上面的完整Demo代码中，有一点是之前没有提到的，就是下面这两句：

```C#
result = new NativeArray<float>(1, Allocator.Persistent);

result.Dispose(); 
```

这里大家可以很明显的注意到，NativeContainer是需要显式管理内存的。关于这方面的内容我会在后面的NativeContainer章节继续跟大家聊。

好，到这里大家应该对C# JobSystem有了一个初步的了解。让我们来做一个小测验，看我们是否真的理解了上面的内容。

下面的代码，输出结果会是什么？

```c#
public class MyCounterJobBehaviour : MonoBehaviour
{
    public struct CounterJob : IJob
    {
        public NativeArray<int> numbers;
        public int result;

        public void Execute()
        {
            for (int i = 0; i < numbers.Length; i++)
            {
                result += numbers[i];
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        var numCount = 10;
        NativeArray<int> numbers = new NativeArray<int>(numCount, Allocator.TempJob);
    
        for (int i = 0; i < numCount; i++)
        {
            numbers[i] = i + 1;
        }

        var jobData = new CounterJob
        {
            numbers = numbers,
            result = 0
        };

        var handle = jobData.Schedule();
        handle.Complete();
        Debug.Log($"result = {jobData.result}");
        numbers.Dispose();
    }
}
```

答案是<span style="background-color:black;color:black">result = 0</span>

这个结果跟你想的一样么？😉

让我们来思考一下为什么是这个结果。我们再来看回顾一下Job的特点：

1. 需要声明成struct
2. struct中的数据必须是blittable的或者是NativeContainer
3. 要实现IJob接口

这些限制条件其实都是为了一个目的，就是要把C#中的Job数据复制到native层，最终由native job system去执行job中的逻辑。想到这其实我们的答案也就显而易见了，Execute()方法中修改的其实只是我们CounterJob的一个副本，并不是原始的CounterJob。因此当我们需要从Job中获得计算结果的时候，我们需要使用``NativeContainer``，否则会得到不正确的结果。下面是正确的写法：

```C#
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class CounterJobBehaviour : MonoBehaviour
{
    public struct CounterJob : IJob
    {
        public NativeArray<int> numbers;
        public NativeArray<int> result;

        public void Execute()
        {
            var tmp = 0;
            for (int i = 0; i < numbers.Length; i++)
            {
                tmp += numbers[i];
            }

            result[0] = tmp;
        }
    }

    void Start()
    {
        var numCount = 10;
        NativeArray<int> numbers = new NativeArray<int>(numCount, Allocator.TempJob);
        var result = new NativeArray<int>(1, Allocator.TempJob);
        
        for (int i = 0; i < numCount; i++)
        {
            numbers[i] = i + 1;
        }

        var jobData = new CounterJob
        {
            numbers = numbers,
            result = result
        };

        var handle = jobData.Schedule();
        handle.Complete();

        Debug.Log($"result = {result[0]}");
        
        result.Dispose();
        numbers.Dispose();
    }
}
```

到这里我们就已经把C# Job System以及IJob大概了解了一下，相信大家应该已经注意到了，IJob只能跑在单一工作（Worker）线程上，如果想要利用全部的工作（Worker）线程就需要用到我们下一节要介绍的另外一个接口了，那就是``IJobFor``。

好，以上就是本节所有的内容了，下一节我们讲继续讨论Job的另一种形式：``IJobFor``。

感谢大家的耐心阅读😙

【文章目录】

1. [什么是C# Job System](https://developer.unity.cn/projects/61f68b70edbc2a16f7df9e83)
2. [IJobFor](https://developer.unity.cn/projects/61f8dbd9edbc2a16f7dfc1d9)
3. [Thread Local](https://developer.unity.cn/projects/61f9e8f0edbc2a16f7dfd115)
4. [Pointers & InterLocked](https://developer.unity.cn/projects/61fa9ecdedbc2a16f7dfe0f6)
5. [Batches & False sharing](https://developer.unity.cn/projects/61fc0a73edbc2a001cf954a3)
6. [Custom batch & Kick jobs](https://developer.unity.cn/projects/61fdd19eedbc2a16f7e01124)
7. SoA vs AoS
