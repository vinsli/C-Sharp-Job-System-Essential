using System;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;

namespace JobSystem.IJobForAdvanced_ParallelCounter
{
    public unsafe class ParallelCounter : MonoBehaviour
    {
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

        // public struct NaiveParallelCounterJob : IJobFor
        // {
        //     [ReadOnly] public NativeArray<int> data;
        //     public NativeReference<int> naiveSum;
        //
        //     public void Execute(int index)
        //     {
        //         naiveSum.Value += data[index];
        //     }
        // }

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

        private static readonly int DATA_COUNT = 100000;
        private NativeArray<int> m_Data;
        private NativeArray<int> m_ThreadLocalSums;

        void Start()
        {
            m_Data = new NativeArray<int>(DATA_COUNT, Allocator.Persistent);

            for (int i = 0; i < DATA_COUNT; i++)
            {
                m_Data[i] = i;
            }

            m_ThreadLocalSums = new NativeArray<int>(JobsUtility.MaxJobThreadCount, Allocator.Persistent);
            ResetThreadLocalSums();
        }

        private void ResetThreadLocalSums()
        {
            for (int i = 0; i < m_ThreadLocalSums.Length; i++)
            {
                m_ThreadLocalSums[i] = 0;
            }
        }

        private void OnDestroy()
        {
            m_Data.Dispose();
            m_ThreadLocalSums.Dispose();
        }

        void Update()
        {
            // var naiveSum = new NativeReference<int>(Allocator.TempJob);
            //
            // new NaiveParallelCounterJob
            // {
            //     data = m_Data,
            //     naiveSum = naiveSum
            // }.ScheduleParallel(m_Data.Length, 64, new JobHandle()).Complete();
            //
            // naiveSum.Dispose();

            var threadLocalCounterJob = new ThreadLocalParallelCounterJob
            {
                data = m_Data,
                sums = m_ThreadLocalSums
            };

            var totalSum = new NativeReference<int>(Allocator.TempJob);

            var totalSumJob = new TotalSumJob
            {
                sums = m_ThreadLocalSums,
                totalSum = totalSum
            };

            var threadLocalCounterJobHandle = threadLocalCounterJob.ScheduleParallel(m_Data.Length, 64, new JobHandle());
            var totalSumJobHandle = totalSumJob.Schedule(threadLocalCounterJobHandle);

            totalSumJobHandle.Complete();

            ResetThreadLocalSums();

            Debug.Log($"[ThreadLocalParallelCounter] Sum = {totalSum.Value}");
            totalSum.Dispose();

            int* sum = (int*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<int>(), UnsafeUtility.AlignOf<int>(), Allocator.TempJob);
            *sum = 0;
            var interlockedParallelCounterJob = new InterlockedParallelCounterJob
            {
                data = m_Data,
                sum = sum
            };

            var interlockedCounterJobHandle = interlockedParallelCounterJob.ScheduleParallel(m_Data.Length, 64, new JobHandle());
            interlockedCounterJobHandle.Complete();

            Debug.Log($"[InterlockedParallelCounterJob] Sum = {*sum}");

            UnsafeUtility.Free(sum, Allocator.TempJob);
        }
    }
}