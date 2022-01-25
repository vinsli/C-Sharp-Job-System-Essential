using System.Text;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace JobSystem.IJobFor_Advanced_ThreadLocal
{
    public class ThreadLocalVelocity : MonoBehaviour
    {
        // struct RandomVelocityJob : IJobFor
        // {
        //     [ReadOnly]
        //     public NativeArray<float> speeds;
        //
        //     [ReadOnly]
        //     [NativeDisableParallelForRestriction]
        //     public NativeArray<Random> randoms;
        //
        //     [NativeSetThreadIndex]
        //     private int m_ThreadIdx;
        //
        //     public float deltaTime;
        //
        //     //output
        //     public NativeArray<float3> positions;
        //
        //     public void Execute(int i)
        //     {
        //         positions[i] += randoms[m_ThreadIdx].NextFloat3Direction() * speeds[i] * deltaTime;
        //     }
        // }
        [BurstCompile]
        struct RandomVelocityJob : IJobFor
        {
            [ReadOnly]
            public NativeArray<float> speeds;
            [ReadOnly]
            public NativeArray<Random> randoms;
            public float deltaTime;
            
            [NativeSetThreadIndex]
            private int m_ThreadIdx;
        
            //output
            public NativeArray<float3> positions;

            public void Execute(int i)
            {
                positions[i] += randoms[m_ThreadIdx].NextFloat3Direction() * speeds[i] * deltaTime;
            }
        }
        
        private static readonly int POSITION_COUNTS = 100000;
        private NativeArray<Random> m_Randoms;
        private NativeArray<float3> m_Positions;
        private NativeArray<float> m_Speeds;
        private JobHandle m_JobHandle;

        private StringBuilder logBuilder = new StringBuilder(512);
    
        void Start()
        {
            m_Randoms = new NativeArray<Random>(JobsUtility.MaxJobThreadCount, Allocator.Persistent);
            for (int i = 0; i < m_Randoms.Length; i++)
            {
                m_Randoms[i] = Random.CreateFromIndex((uint)i);
            }
            
            m_Positions= new NativeArray<float3>(POSITION_COUNTS, Allocator.Persistent);
            m_Speeds = new NativeArray<float>(POSITION_COUNTS, Allocator.Persistent);
            for (var i = 0; i < m_Speeds.Length; i++)
                m_Speeds[i] = m_Randoms[i % m_Randoms.Length].NextFloat(0, 100);
        }

        private void OnDestroy()
        {
            m_JobHandle.Complete();
            m_Randoms.Dispose();
            m_Positions.Dispose();
            m_Speeds.Dispose();
        }
    
        void Update()
        {
            m_JobHandle.Complete();
            // logBuilder.Clear();
            //
            // for (int i = 0; i < m_Positions.Length; i++)
            // {
            //    logBuilder.AppendLine($"Pos[{i}] = {m_Positions[i]}");
            // }
            //
            // Debug.Log(logBuilder.ToString());
            
            // Initialize the job data
            // var random = new Random(1234);
            var randomVelocityJob = new RandomVelocityJob
            {
                speeds = m_Speeds,
                randoms = m_Randoms,
                positions = m_Positions,
                deltaTime = Time.deltaTime,
            };
        
            m_JobHandle = randomVelocityJob.ScheduleParallel(m_Positions.Length, 64, m_JobHandle);
        }
    }
}
