using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Profiling;

namespace JobSystem.LoopVectorization
{
    public class LoopVectorizatiion : MonoBehaviour
    {
        public struct FooData
        {
            public float a;
            public float b;
            public float c;
            public float d;
        }

        private static readonly int DATA_COUNT = 1000000;
        private FooData[] _managedFooDatas;
        private FooData[] _managedFooResults;
        private NativeArray<FooData> _fooDatas;
        private NativeArray<FooData> _fooVectorDatas;
        private NativeArray<FooData> _fooResults;

        void Start()
        {
            _managedFooDatas = new FooData[DATA_COUNT];
            _managedFooResults = new FooData[DATA_COUNT];
            _fooDatas = new NativeArray<FooData>(DATA_COUNT, Allocator.Persistent);
            _fooVectorDatas = new NativeArray<FooData>(DATA_COUNT, Allocator.Persistent);
            _fooResults = new NativeArray<FooData>(DATA_COUNT, Allocator.Persistent);

            for (int i = 0; i < _managedFooDatas.Length; i++)
            {
                _managedFooDatas[i] = new FooData
                {
                    a = i,
                    b = i + 1,
                    c = i + 2,
                    d = i + 3
                };
            }

            for (int i = 0; i < _fooDatas.Length; i++)
            {
                _fooDatas[i] = new FooData
                {
                    a = i,
                    b = i + 1,
                    c = i + 2,
                    d = i + 3
                };
            }

            for (int i = 0; i < _fooVectorDatas.Length; i++)
            {
                _fooVectorDatas[i] = new FooData
                {
                    a = i,
                    b = -(i + 1),
                    c = i + 2,
                    d = -(i + 3)
                };
            }
        }

        void Update()
        {
            MainThreadManagedFooDataCal();
            MainThreadFooDataCal();
            MainThreadFooDataCalNoBlit();
            
            new FooDataJob
            {
                fooDatas = _fooDatas,
                fooResults = _fooResults
            }.Run();

            new FooDataJobNoAlias
            {
                fooDatas = _fooDatas,
                fooResults = _fooResults
            }.Run();

            new FooVectorDataJobNoAlias
            {
                fooDatas = _fooVectorDatas,
                fooResults = _fooResults
            }.Run();
            
            new FooVectorDataParallelJobNoAlias
            {
                fooDatas = _fooVectorDatas,
                fooResults = _fooResults
            }.ScheduleBatch(_fooDatas.Length * 4, 262144).Complete();
            
            // new FooDataJobNoBlit
            // {
            //     fooDatas = _fooDatas,
            //     fooResults = _fooResults
            // }.Run();
        }

        private void MainThreadFooDataCal()
        {
            Profiler.BeginSample("NativeFooDataCal");
            for (int i = 0; i < _fooDatas.Length; i++)
            {
                var data = _fooDatas[i];
                var result = _fooResults[i];

                result.a += data.a;
                result.b -= data.b;
                result.c += data.c;
                result.d -= data.d;

                _fooResults[i] = result;
            }
            Profiler.EndSample();
        }

        private unsafe void MainThreadFooDataCalNoBlit()
        {
            Profiler.BeginSample("NativeFooDataCalNoBlit");
            var fooDataPtr = (FooData*) _fooDatas.GetUnsafeReadOnlyPtr();
            var fooResultPtr = (FooData*) _fooResults.GetUnsafePtr();
            for (int i = 0; i < _fooDatas.Length; i++)
            {
                ref var data = ref fooDataPtr[i];
                ref var result = ref fooResultPtr[i];
            
                result.a += data.a;
                result.b -= data.b;
                result.c += data.c;
                result.d -= data.d;
            }
            Profiler.EndSample();
        }

        private void MainThreadManagedFooDataCal()
        {
            Profiler.BeginSample("ManagedFooDataCal");
            for (int i = 0; i < _managedFooDatas.Length; i++)
            {
                var data = _managedFooDatas[i];
                var result = _managedFooResults[i];
            
                result.a += data.a;
                result.b -= data.b;
                result.c += data.c;
                result.d -= data.d;
            
                _managedFooResults[i] = result;
            }
            Profiler.EndSample();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Calculate(ref FooData data, ref FooData result)
        {
            result.a += data.a;
            result.b -= data.b;
            result.c += data.c;
            result.d -= data.d;
        }

        [BurstCompile]
        private unsafe struct FooDataJob : IJob
        {
            public NativeArray<FooData> fooDatas;
            public NativeArray<FooData> fooResults;

            public void Execute()
            {
                var fooDataPtr = (FooData*) fooDatas.GetUnsafeReadOnlyPtr();
                var fooResultPtr = (FooData*) fooResults.GetUnsafePtr();
                Do(fooDataPtr, fooResultPtr, fooDatas.Length);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private void Do(FooData* datas, FooData* results, int count)
            {
                for (int i = 0; i < count; i++)
                {
                    ref var data = ref datas[i];
                    ref var result = ref results[i];
                    result.a += data.a;
                    result.b -= data.b;
                    result.c += data.c;
                    result.d -= data.d;
                }
            }
        }

        [BurstCompile]
        private unsafe struct FooDataJobNoBlit : IJob
        {
            public NativeArray<FooData> fooDatas;
            public NativeArray<FooData> fooResults;

            public void Execute()
            {
                var datas = (FooData*) fooDatas.GetUnsafeReadOnlyPtr();
                var results = (FooData*) fooResults.GetUnsafePtr();

                for (int i = 0; i < fooDatas.Length; i++)
                {
                    ref var data = ref datas[i];
                    ref var result = ref results[i];

                    Calculate(ref data, ref result);
                }
            }
        }

        [BurstCompile]
        private unsafe struct FooDataJobNoAlias : IJob
        {
            public NativeArray<FooData> fooDatas;
            public NativeArray<FooData> fooResults;

            public void Execute()
            {
                var datas = (FooData*) fooDatas.GetUnsafeReadOnlyPtr();
                var results = (FooData*) fooResults.GetUnsafePtr();
                Do(datas, results, fooDatas.Length);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private void Do([NoAlias] FooData* datas, [NoAlias] FooData* results, int count)
            {
                for (int i = 0; i < count; i++)
                {
                    ref var data = ref datas[i];
                    ref var result = ref results[i];

                    result.a += data.a;
                    result.b -= data.b;
                    result.c += data.c;
                    result.d -= data.d;
                }
            }
        }

        [BurstCompile]
        private unsafe struct FooVectorDataJobNoAlias : IJob
        {
            public NativeArray<FooData> fooDatas;
            public NativeArray<FooData> fooResults;

            public void Execute()
            {
                var datas = (float*) fooDatas.GetUnsafeReadOnlyPtr();
                var results = (float*) fooResults.GetUnsafePtr();
                Do(datas, results, fooDatas.Length * 4);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private void Do([NoAlias] float* datas, [NoAlias] float* results, int count)
            {
                for (int i = 0; i < count; i++)
                {
                    results[i] += datas[i];
                }
            }
        }
        
        [BurstCompile]
        private unsafe struct FooVectorDataParallelJobNoAlias : IJobParallelForBatch
        {
            public NativeArray<FooData> fooDatas;
            public NativeArray<FooData> fooResults;

            public void Execute(int startIndex, int count)
            {
                var datas = (float*) fooDatas.GetUnsafeReadOnlyPtr();
                var results = (float*) fooResults.GetUnsafePtr();
                Do(datas, results, count);
            }
            
            [MethodImpl(MethodImplOptions.NoInlining)]
            private void Do([NoAlias] float* datas, [NoAlias] float* results, int count)
            {
                for (int i = 0; i < count; i++)
                {
                    results[i] += datas[i];
                }
            }
        }

        [BurstCompile]
        private struct CopyJob : IJob
        {
            [ReadOnly] public NativeArray<float> Input;

            [WriteOnly] public NativeArray<float> Output;

            public void Execute()
            {
                for (int i = 0; i < Input.Length; i++)
                {
                    Output[i] += Input[i];
                }
            }
        }
    }
}