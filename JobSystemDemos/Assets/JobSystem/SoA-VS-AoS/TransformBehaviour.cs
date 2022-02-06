using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace JobSystem.SoA_VS_AoS
{
    public unsafe class TransformBehaviour : MonoBehaviour
    {
        [BurstCompile]
        public struct TransformAoSJob : IJobFor
        {
            public NativeArray<TransformAoS> transformAoSes;
            [ReadOnly] public float3 velocity;
            [ReadOnly] public float deltaTime;

            public void Execute(int index)
            {
                // var transAoS = transformAoSes[index];
                // transAoS.position += velocity * deltaTime;
                // transformAoSes[index] = transAoS;

                var transformPtr = (TransformAoS*)transformAoSes.GetUnsafePtr();
                ref var transform = ref transformPtr[index];
                transform.position += velocity * deltaTime;
            }
        }

        [BurstCompile]
        public struct TransformSoAJob : IJobFor
        {
            public NativeArray<float3> positions;
            [ReadOnly] public float3 velocity;
            [ReadOnly] public float deltaTime;

            public void Execute(int index)
            {
                var positionPtr = (float3*)positions.GetUnsafePtr();
                ref var position = ref positionPtr[index];
                position += velocity * deltaTime;
            }
        }
        
        private NativeArray<TransformAoS> m_TransformAoSes;
        private TransformSoA m_TransformSoA;
        private float3 m_Velocity;

        private static readonly int TRANSFORM_COUNT = 5000000;

        private void Start()
        {
            m_TransformAoSes = new NativeArray<TransformAoS>(TRANSFORM_COUNT, Allocator.Persistent);
            m_TransformSoA = new TransformSoA(TRANSFORM_COUNT);

            var transformAoSPtr = (TransformAoS*)m_TransformAoSes.GetUnsafePtr();

            var rand = new Random(1332);

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
            }
        }

        private void OnDestroy()
        {
            m_TransformAoSes.Dispose();
            m_TransformSoA.Dispose();
        }

        private void Update()
        {
            new TransformSoAJob
            {
                positions = m_TransformSoA.positions,
                velocity = m_Velocity,
                deltaTime = Time.deltaTime
            }.ScheduleParallel(m_TransformSoA.positions.Length, 64, new JobHandle()).Complete();
            
            new TransformAoSJob
            {
                transformAoSes = m_TransformAoSes,
                velocity = m_Velocity,
                deltaTime = Time.deltaTime
            }.ScheduleParallel(m_TransformAoSes.Length, 64, new JobHandle()).Complete();
        }

        struct AoSData
        {
            public int a;
            public int b;
            public int c;
            public int d;
        }

        struct SoAData
        {
            public NativeArray<int> aArray;
            public NativeArray<int> bArray;
            public NativeArray<int> cArray;
            public NativeArray<int> dArray;
        }
    }
}