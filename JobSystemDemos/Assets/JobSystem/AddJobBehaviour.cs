using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace JobSystem
{
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
}