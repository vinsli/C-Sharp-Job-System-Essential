using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using Random = Unity.Mathematics.Random;

namespace JobSystem.IJobForAdvanced_CustomBatches
{
    public class CustomBatches : MonoBehaviour
    {
        struct HierarchicalTransformJob : IJobFor
        {
            [ReadOnly] public NativeArray<float3> rootPositions;
            [ReadOnly] public NativeArray<quaternion> rootRotations;

            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> childLocalPositions;

            [NativeDisableContainerSafetyRestriction]
            public NativeArray<quaternion> childLocalRotations;

            public void Execute(int i)
            {
                var parentPos = rootPositions[i];
                var parentRot = rootRotations[i];

                for (int j = 0; j < 5; j++)
                {
                    var index = i * 5 + j;

                    parentPos += math.mul(parentPos, childLocalPositions[index]);
                    parentRot = math.mul(parentRot, childLocalRotations[index]);

                    childLocalPositions[index] = parentPos;
                    childLocalRotations[index] = parentRot;
                }
            }
        }

        private static readonly int POSITION_COUNT = 1000;
        private NativeArray<float3> m_RootPositions;
        private NativeArray<quaternion> m_RootRotations;

        private NativeArray<float3> m_LocalPositions;
        private NativeArray<quaternion> m_LocalRotations;

        private JobHandle m_JobHandle;

        private void Start()
        {
            m_RootPositions = new NativeArray<float3>(POSITION_COUNT, Allocator.Persistent);
            m_RootRotations = new NativeArray<quaternion>(POSITION_COUNT, Allocator.Persistent);
            m_LocalPositions = new NativeArray<float3>(POSITION_COUNT * 5, Allocator.Persistent);
            m_LocalRotations = new NativeArray<quaternion>(POSITION_COUNT * 5, Allocator.Persistent);

            var rand = new Random(7632);
            for (var i = 0; i < POSITION_COUNT; i++)
            {
                m_RootPositions[i] = rand.NextFloat3();
                m_RootRotations[i] = rand.NextQuaternionRotation();

                var localIndex = i * 5;
                for (int j = 0; j < 5; j++)
                {
                    m_LocalPositions[localIndex + j] = rand.NextFloat3();
                    m_LocalRotations[localIndex + j] = rand.NextQuaternionRotation();
                }
            }
        }

        private void OnDestroy()
        {
            m_RootPositions.Dispose();
            m_RootRotations.Dispose();

            m_LocalPositions.Dispose();
            m_LocalRotations.Dispose();
        }

        private void Update()
        {
            m_JobHandle.Complete();
            var job = new HierarchicalTransformJob()
            {
                rootPositions = m_RootPositions,
                rootRotations = m_RootRotations,

                childLocalPositions = m_LocalPositions,
                childLocalRotations = m_LocalRotations
            };
            
            Profiler.BeginSample("HierarchicalTransformJob");
            m_JobHandle = job.ScheduleParallel(m_RootPositions.Length, 64, new JobHandle());
            // m_JobHandle.Complete();
            JobHandle.ScheduleBatchedJobs();
            Profiler.EndSample();
        }
    }
}