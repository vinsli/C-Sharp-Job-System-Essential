using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class IJobForBehaviour : MonoBehaviour
{
    struct AddJob : IJobFor
    {
        public NativeArray<int> termsLeft;
        public NativeArray<int> termsRight;
        public NativeArray<int> sums;

        public void Execute(int index)
        {
            sums[index] = termsLeft[index] + termsRight[index];
        }
    }

    private void Start()
    {
        int numCount = 10;
        var termsLeft = new NativeArray<int>(numCount, Allocator.TempJob);
        var termsRight = new NativeArray<int>(numCount, Allocator.TempJob);
        var sums = new NativeArray<int>(numCount, Allocator.TempJob);

        var addJob = new AddJob
        {
            termsLeft = termsLeft,
            termsRight = termsRight,
            sums = sums
        };
        
        addJob.Run(numCount);
        var handle = addJob.Schedule(numCount, new JobHandle());
        handle = addJob.ScheduleParallel(numCount, 1, handle);
        
        handle.Complete();

        for (int i = 0; i < numCount; i++)
        {
            Debug.Log($"Sum[{i}] = {sums[i]}");
        }
        
        termsLeft.Dispose();
        termsRight.Dispose();
        sums.Dispose();
    }
}
