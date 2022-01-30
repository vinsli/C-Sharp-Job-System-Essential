using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;

namespace JobSystem.IJobForAdvanced_Batches
{
    public class Batches : MonoBehaviour
    {
        struct VelocityJob : IJobFor
        {
            [ReadOnly] public NativeArray<float3> velocity;
            public NativeArray<float3> position;

            public float deltaTime;

            public void Execute(int i)
            {
                // Move the positions based on delta time and velocity
                position[i] += velocity[i] * deltaTime;
            }
        }

        private static readonly int POSITION_COUNT = 5000000;
        private NativeArray<float3> m_Positions;
        private NativeArray<float3> m_Velocity;

        private void Start()
        {
            m_Positions = new NativeArray<float3>(POSITION_COUNT, Allocator.Persistent);

            m_Velocity = new NativeArray<float3>(POSITION_COUNT, Allocator.Persistent);
            for (var i = 0; i < m_Velocity.Length; i++)
                m_Velocity[i] = new float3(0, 10, 0);
        }

        private void OnDestroy()
        {
            m_Positions.Dispose();
            m_Velocity.Dispose();
        }

        private void Update()
        {
            var job = new VelocityJob()
            {
                deltaTime = Time.deltaTime,
                position = m_Positions,
                velocity = m_Velocity
            };
            var batchCount = 1;
            Profiler.BeginSample($"Batch = {batchCount}");
            job.ScheduleParallel(m_Positions.Length, batchCount, new JobHandle()).Complete();
            Profiler.EndSample();

            batchCount = 10;
            Profiler.BeginSample($"Batch = {batchCount}");
            job.ScheduleParallel(m_Positions.Length, batchCount, new JobHandle()).Complete();
            Profiler.EndSample();
            
            Debug.Log($"Cache line size = {JobsUtility.CacheLineSize}");
            // var batchCount = JobsUtility.CacheLineSize * UnsafeUtility.SizeOf<float3>() ;

            batchCount = 16;
            Profiler.BeginSample($"Batch = {batchCount}");
            job.ScheduleParallel(m_Positions.Length, batchCount, new JobHandle()).Complete();
            Profiler.EndSample();
            
            batchCount = 32;
            Profiler.BeginSample($"Batch = {batchCount}");
            job.ScheduleParallel(m_Positions.Length, batchCount, new JobHandle()).Complete();
            Profiler.EndSample();
            
            batchCount = 64;
            Profiler.BeginSample($"Batch = {batchCount}");
            job.ScheduleParallel(m_Positions.Length, batchCount, new JobHandle()).Complete();
            Profiler.EndSample();
        }
    }
}