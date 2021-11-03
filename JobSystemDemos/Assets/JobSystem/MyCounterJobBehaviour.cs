using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace JobSystem
{
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
}