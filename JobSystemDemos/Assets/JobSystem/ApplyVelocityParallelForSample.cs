using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Profiling;
using Random = Unity.Mathematics.Random;

class ApplyVelocityParallelForSample : MonoBehaviour
{
    struct VelocityJob : IJobFor
    {
        [ReadOnly]
        public NativeArray<Vector3> velocity;
        public NativeArray<Vector3> position;

        public float deltaTime;
        
        public void Execute(int i)
        {
            // Move the positions based on delta time and velocity
            position[i] = position[i] + velocity[i] * deltaTime;
        }
    }

    public void Update()
    {
        var position = new NativeArray<Vector3>(50000, Allocator.Persistent);

        var velocity = new NativeArray<Vector3>(50000, Allocator.Persistent);
        for (var i = 0; i < velocity.Length; i++)
            velocity[i] = new Vector3(0, 10, 0);

        // Initialize the job data
        var job = new VelocityJob()
        {
            deltaTime = Time.deltaTime,
            position = position,
            velocity = velocity
        };
        
        Profiler.BeginSample("Job.Run");
        // Schedule job to run immediately on main thread. First parameter is how many for-each iterations to perform.
        job.Run(position.Length);
        Profiler.EndSample();
        
        Profiler.BeginSample("Job.Schedule");
        JobHandle scheduleJobDependency = new JobHandle();
        JobHandle scheduleJobHandle = job.Schedule(position.Length, scheduleJobDependency);
        scheduleJobHandle.Complete();
        Profiler.EndSample();
        
        Profiler.BeginSample("Job.ScheduleParallel");
        JobHandle scheduleParallelJobHandle = job.ScheduleParallel(position.Length, 64, scheduleJobHandle);
        scheduleParallelJobHandle.Complete();
        Profiler.EndSample();

        
        Debug.Log(job.position[0]);

        // Native arrays must be disposed manually.
        position.Dispose();
        velocity.Dispose();
    }
}