using System;
using Unity.Collections;
using Unity.Mathematics;

namespace JobSystem.SoA_VS_AoS
{
    public class TransformSoA : IDisposable
    {
        public NativeArray<float3> positions;
        public NativeArray<quaternion> rotations;
        public NativeArray<float3> scales;

        public TransformSoA(int count)
        {
            Create(count);
        }

        private void Create(int count)
        {
            positions = new NativeArray<float3>(count, Allocator.Persistent);
            rotations = new NativeArray<quaternion>(count, Allocator.Persistent);
            scales = new NativeArray<float3>(count, Allocator.Persistent);
        }

        public void Dispose()
        {
            if (positions.IsCreated)
                positions.Dispose();

            if (rotations.IsCreated)
                rotations.Dispose();

            if (scales.IsCreated)
                scales.Dispose();
        }
    }
}