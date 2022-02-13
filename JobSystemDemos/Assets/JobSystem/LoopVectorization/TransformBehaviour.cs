using System.Runtime.CompilerServices;
using JobSystem.SoA_VS_AoS;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace JobSystem.LoopVectorization
{
    public unsafe class TransformBehaviour : MonoBehaviour
    {
        [BurstCompile]
        public struct TransformAoSJob : IJobFor
        {
            public NativeArray<TransformAoS> transformAoSes;
            [ReadOnly] public NativeArray<float3> velocities;
            [ReadOnly] public float deltaTime;

            public void Execute(int index)
            {
                // var transAoS = transformAoSes[index];
                // transAoS.position += velocity * deltaTime;
                // transformAoSes[index] = transAoS;

                var transformPtr = (TransformAoS*) transformAoSes.GetUnsafePtr();
                ref var transform = ref transformPtr[index];
                transform.position += velocities[index] * deltaTime;
            }
        }
        
        [BurstCompile]
        public struct TransformSoAJob : IJob
        {
            public NativeArray<float3> positions;
            [ReadOnly] public NativeArray<float3> velocities;
            [ReadOnly] public float deltaTime;

            public void Execute()
            {
                for (int i = 0; i < positions.Length; i++)
                {
                    positions[i] += velocities[i] * deltaTime;
                }
            }
        }

        [BurstCompile]
        public struct TransformSoAParallel : IJobFor
        {
            public NativeArray<float3> positions;
            [ReadOnly] public NativeArray<float3> velocities;
            [ReadOnly] public float deltaTime;

            public void Execute(int index)
            {
                positions[index] += velocities[index] * deltaTime;
                
                // var positionPtr = (float3*) positions.GetUnsafePtr();
                // ref var position = ref positionPtr[index];
                // position += velocities[index] * deltaTime;
            }
        }

        [BurstCompile]
        public struct TransformSoAVectorized : IJob
        {
            [NoAlias] public NativeArray<float3> positions;
            [ReadOnly] public NativeArray<float3> velocities;
            [ReadOnly] public float deltaTime;
        
            public void Execute()
            {
                var positionsPtr = (float*)positions.GetUnsafePtr();
                var velocityPtr = (float*) velocities.GetUnsafeReadOnlyPtr();

                for (int i = 0; i < positions.Length * 3; i++)
                {
                    positionsPtr[i] += velocityPtr[i] * deltaTime;
                }
            }
        }

        [BurstCompile]
        public struct TransformSoAJobParallelVectorized : IJobParallelForBatch
        {
            public NativeArray<float3> positions;
            [ReadOnly] public NativeArray<float3> velocities;
            [ReadOnly] public float deltaTime;

            public void Execute(int startIndex, int count)
            {
                var positionsPtr = (float*) positions.GetUnsafePtr() + startIndex * 3;
                var velocityPtr = (float*) velocities.GetUnsafeReadOnlyPtr() + startIndex * 3;
                
                for (int i = 0; i < count * 3; i++)
                {
                    positionsPtr[i] += velocityPtr[i] * deltaTime;
                }
            }
        }

        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // private static void Do([NoAlias] float3* positions, int count, [NoAlias] float3* velocity, float deltaTime)
        // {
        //     for (int i = 0; i < count; i++)
        //     {
        //         positions[i] += velocity[i] * deltaTime;
        //
        //         // ref var pos = ref positions[i];
        //         // ref var velo = ref velocity[i];
        //         // pos += velo * deltaTime;
        //     }
        // }
        
        [BurstCompile]
        public struct TransformSoAPackedParallelVectorized : IJobParallelForBatch
        {
            public NativeArray<float4> positions;
            [ReadOnly] public NativeArray<float4> velocities;
            [ReadOnly] public float deltaTime;

            public void Execute(int startIndex, int count)
            {
                
                var positionsPtr = (float*)positions.GetUnsafePtr() + startIndex * 4;
                var velocityPtr = (float*)velocities.GetUnsafeReadOnlyPtr() + startIndex * 4;
                
                var loopCount = count * 4;
                Hint.Assume(loopCount >= 32);
                Hint.Assume(loopCount % 32 == 0);
                
                for (int i = 0; i < loopCount; i++)
                {
                    positionsPtr[i] += velocityPtr[i] * deltaTime;
                }
            }
        }

        // [MethodImpl(MethodImplOptions.NoInlining)]
        // private static void VectorizedLoop([NoAlias] float* positions, int count, [NoAlias] float* velocity, float deltaTime)
        // {
        //     for (int i = 0; i < count; i++)
        //     {
        //         // ref var position = ref positions[i];
        //         // position += velocity[i] * deltaTime;
        //         
        //         positions[i] += velocity[i] * deltaTime;
        //     }
        // }
        
        [BurstCompile]
        public struct TransformSoAPackedParallel : IJobFor
        {
            public NativeArray<float4> positions;
            [ReadOnly] public NativeArray<float4> velocities;
            [ReadOnly] public float deltaTime;

            public void Execute(int index)
            {
                positions[index] += velocities[index] * deltaTime;
                
                // var positionPtr = (float4*) positions.GetUnsafePtr();
                // ref var position = ref positionPtr[index];
                // position += velocities[index] * deltaTime;
            }
        }
        
        [BurstCompile]
        public struct TransformSoAPacked : IJob
        {
            public NativeArray<float4> positions;
            [ReadOnly] public NativeArray<float4> velocities;
            [ReadOnly] public float deltaTime;

            public void Execute()
            {
                for (int i = 0; i < positions.Length; i++)
                {
                    positions[i] += velocities[i] * deltaTime;
                }
            }
        }
        
        [BurstCompile]
        public struct TransformSoAPackedVectorized : IJob
        {
            public NativeArray<float4> positions;
            [ReadOnly] public NativeArray<float4> velocities;
            [ReadOnly] public float deltaTime;

            public void Execute()
            {
                var positionPtr = (float*) positions.GetUnsafePtr();
                var velocityPtr = (float*) velocities.GetUnsafeReadOnlyPtr();
                
                var loopCount = positions.Length * 4;
                Hint.Assume(loopCount >= 32);
                Hint.Assume(loopCount % 32 == 0);
                
                for (int i = 0; i < loopCount; i++)
                {
                    positionPtr[i] += velocityPtr[i] * deltaTime;
                }
            }
        }

        private NativeArray<TransformAoS> m_TransformAoSes;
        private TransformSoA m_TransformSoA;
        private float3 m_Velocity;
        private NativeArray<float3> m_Velocities;
        
        private NativeArray<float4> m_PackedPositions;
        private NativeArray<float4> m_PackedVelocities;

        private static readonly int TRANSFORM_COUNT = 5000000;

        private void Start()
        {
            m_TransformAoSes = new NativeArray<TransformAoS>(TRANSFORM_COUNT, Allocator.Persistent);
            m_TransformSoA = new TransformSoA(TRANSFORM_COUNT);
            m_Velocities = new NativeArray<float3>(TRANSFORM_COUNT, Allocator.Persistent);
            

            var transformAoSPtr = (TransformAoS*) m_TransformAoSes.GetUnsafePtr();

            var rand = new Random(1332);
            
            var packedCount = TRANSFORM_COUNT * 3 / 4;
            m_PackedPositions = new NativeArray<float4>(packedCount, Allocator.Persistent);
            m_PackedVelocities = new NativeArray<float4>(packedCount, Allocator.Persistent);

            for (int i = 0; i < packedCount; i++)
            {
                m_PackedPositions[i] = rand.NextFloat4();
                m_PackedVelocities[i] = rand.NextFloat4();
            }

            m_Velocity = rand.NextFloat3Direction();

            for (int i = 0; i < TRANSFORM_COUNT; i++)
            {
                ref var transAoS = ref transformAoSPtr[i];
                transAoS.position = rand.NextFloat3();
                transAoS.rotation = rand.NextQuaternionRotation();
                transAoS.scale = new float3(1, 1, 1);

                m_TransformSoA.positions[i] = rand.NextFloat3();
                m_TransformSoA.rotations[i] = rand.NextQuaternionRotation();
                m_TransformSoA.scales[i] = new float3(1, 1, 1);

                m_Velocities[i] = rand.NextFloat3Direction() * rand.NextFloat3();
            }
        }

        private void OnDestroy()
        {
            m_TransformAoSes.Dispose();
            m_TransformSoA.Dispose();
            m_Velocities.Dispose();
            m_PackedPositions.Dispose();
            m_PackedVelocities.Dispose();
        }

        private void Update()
        {
            new TransformSoAJob
            {
                positions = m_TransformSoA.positions,
                velocities = m_Velocities,
                deltaTime = Time.deltaTime
            }.Schedule().Complete();
            
            new TransformSoAPacked
            {
                positions = m_PackedPositions,
                velocities = m_PackedVelocities,
                deltaTime = Time.deltaTime
            }.Schedule().Complete();
            
            new TransformSoAVectorized
            {
                positions = m_TransformSoA.positions,
                velocities = m_Velocities,
                deltaTime = Time.deltaTime
            }.Schedule().Complete();
            
            new TransformSoAPackedVectorized
            {
                positions = m_PackedPositions,
                velocities = m_PackedVelocities,
                deltaTime = Time.deltaTime
            }.Schedule().Complete();
            
            new TransformSoAPackedParallel
            {
                positions = m_PackedPositions,
                velocities = m_PackedVelocities,
                deltaTime = Time.deltaTime
            }.ScheduleParallel(m_PackedPositions.Length, 10000, default).Complete();
            
            new TransformSoAParallel
            {
                positions = m_TransformSoA.positions,
                velocities = m_Velocities,
                deltaTime = Time.deltaTime
            }.ScheduleParallel(m_TransformSoA.positions.Length, 10000, default).Complete();
            
            new TransformSoAPackedParallelVectorized
            {
                positions = m_PackedPositions,
                velocities = m_PackedVelocities,
                deltaTime = Time.deltaTime
            }.ScheduleBatch(m_PackedPositions.Length, 64).Complete();
            
            new TransformSoAJobParallelVectorized
            {
                positions = m_TransformSoA.positions,
                velocities = m_Velocities,
                deltaTime = Time.deltaTime
            }.ScheduleBatch(m_TransformSoA.positions.Length, 64).Complete();


            JobsUtility.JobWorkerCount = 8;
            new TransformSoAParallel
            {
                positions = m_TransformSoA.positions,
                velocities = m_Velocities,
                deltaTime = Time.deltaTime
            }.ScheduleParallel(m_TransformSoA.positions.Length, 10000, default).Complete();
            
            new TransformSoAPackedParallel
            {
                positions = m_PackedPositions,
                velocities = m_PackedVelocities,
                deltaTime = Time.deltaTime
            }.ScheduleParallel(m_PackedPositions.Length, 10000, default).Complete();
            
            new TransformSoAJobParallelVectorized
            {
                positions = m_TransformSoA.positions,
                velocities = m_Velocities,
                deltaTime = Time.deltaTime
            }.ScheduleBatch(m_TransformSoA.positions.Length, 10000).Complete();
            
            new TransformSoAPackedParallelVectorized
            {
                positions = m_PackedPositions,
                velocities = m_PackedVelocities,
                deltaTime = Time.deltaTime
            }.ScheduleBatch(m_PackedPositions.Length, 10000).Complete();
            
            JobsUtility.ResetJobWorkerCount();
            
            // new TransformSoAJobVectorized
            // {
            //     positions = m_TransformSoA.positions,
            //     velocities = m_Velocities,
            //     deltaTime = Time.deltaTime
            // }.Schedule().Complete();
            //
            // new TransformAoSJob
            // {
            //     transformAoSes = m_TransformAoSes,
            //     velocities = m_Velocities,
            //     deltaTime = Time.deltaTime
            // }.ScheduleParallel(m_TransformAoSes.Length, 64, new JobHandle()).Complete();
        }
    }
}