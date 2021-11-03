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