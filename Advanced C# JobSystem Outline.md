1. C# Job System特点
    1. 
2. 什么是job
    1. 创建job
    2. 执行job
3. Native Container 简介
   1. NativeArray
   2. NativeList
   3. NativeHashMap
   4. NativeMultiHashMap
   5. NativeContainerAttribute
   6. DeallocateOnJobCompletionAttribute
   7. 
4. workerthread
    1. workerthread数量如何定义的
    2. 如何控制同时工作的workerthread的数量
    3. 
5. schedule jobs
    1. 如何kick jobs
    2. dependency
    3. 
6. safety system
    1. blittable data
    2. 内存对齐

7. 高级NativeContainer
   1. [ Custom native container ](https://dotsplayground.com/2020/03/customnativecontainerpt1/)
8. 高级Jobs
    1. Job types
    2. 存指针
    3. stackalloc
    4. struct blit / ref return
    5. 停止safty check
    6. 获得当前worker id
    7. cache line
9.  SOA vs AOS

10. Burst