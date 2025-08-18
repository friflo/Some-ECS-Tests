using Friflo.Engine.ECS;

namespace RouderSky;

public static class TestFrifloECSPerformance
{
    private const int ENTITY_COUNT = 100_000;
    private const int SMALL_ENTITY_COUNT = 10_000;
    private const int ITERATION_COUNT = 10;
    
    public struct TestComponent : IComponent 
    { 
        public int value; 
        public float data;
    }
    
    public struct TestComponent2 : IComponent 
    { 
        public System.Numerics.Vector3 position; 
    }
    
    public struct TestTag : ITag { }
    
    public struct IndexedTestComponent : IIndexedComponent<int>
    {
        public int id;
        public float value;
        public int GetIndexedValue() => id;
    }

    // 测试实体创建性能：逐个创建 vs 批量创建
    public static void TestEntityCreationPerformance()
    {
        DebugMgr.LogInfo(() => "=== Entity Creation Performance Test ===");
        
        // 测试逐个创建实体并添加组件
        var sw = System.Diagnostics.Stopwatch.StartNew();
        EntityStore store1 = new EntityStore();
        for (int i = 0; i < ENTITY_COUNT; i++)
        {
            Entity entity = store1.CreateEntity();
            entity.AddComponent(new TestComponent { value = i });
            entity.AddComponent(new Position());
            entity.AddTag<TestTag>();
        }
        sw.Stop();
        long individualCreationTime = sw.ElapsedMilliseconds;
        
        // 测试逐个创建实体并添加组件
        sw.Restart();
        EntityStore store2 = new EntityStore();
        for (int i = 0; i < ENTITY_COUNT; i++)
        {
            store2.CreateEntity(new TestComponent { value = i }, new Position(), Tags.Get<TestTag>());
        }
        sw.Stop();
        long individualWithComponentsTime = sw.ElapsedMilliseconds;
        
        // 测试批量创建相同Archetype的实体
        sw.Restart();
        EntityStore store3 = new EntityStore();
        Archetype archetype = store3.GetArchetype(ComponentTypes.Get<TestComponent, Position>(), Tags.Get<TestTag>());
        var entities = archetype.CreateEntities(ENTITY_COUNT);
        // 批量添加组件值
        for (int i = 0; i < entities.Count; i++)
        {
            entities[i].AddComponent(new TestComponent { value = i });
        }
        sw.Stop();
        long batchCreationTime = sw.ElapsedMilliseconds;
        
        DebugMgr.LogInfo(() => $"Individual Creation: {individualCreationTime}ms");
        DebugMgr.LogInfo(() => $"Individual + Components: {individualWithComponentsTime}ms");
        DebugMgr.LogInfo(() => $"Batch Creation: {batchCreationTime}ms");
    }

    // 测试组件批量操作性能
    public static void TestBatchOperationsPerformance()
    {
        DebugMgr.LogInfo(() => "=== Batch Operations Performance Test ===");

        EntityStore store = new EntityStore();
        var entities = new Entity[ENTITY_COUNT];
        for (int i = 0; i < entities.Length; i++)
        {
            entities[i] = store.CreateEntity();
        }

        // 测试逐个添加组件
        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < entities.Length; i++)
        {
            entities[i].AddComponent(new TestComponent { value = i });
            entities[i].AddComponent(new Position(i, i, i));
            entities[i].AddTag<TestTag>();
        }
        sw.Stop();
        long individualAddTime = sw.ElapsedMilliseconds;

        // 重置实体
        for (int i = 0; i < entities.Length; i++)
        {
            entities[i].DeleteEntity();
            entities[i] = store.CreateEntity();
        }

        // 测试批量添加组件
        // wht que 为什么这里是最快的？
        //      https://github.com/friflo/friflo-ecs-unity/issues/11
        sw.Restart();
        for (int i = 0; i < entities.Length; i++)
        {
            entities[i].Add(
                new TestComponent { value = i },
                new Position(i, i, i),
                Tags.Get<TestTag>());
        }
        sw.Stop();
        long batchAddTime = sw.ElapsedMilliseconds;

        // 重置实体
        for (int i = 0; i < entities.Length; i++)
        {
            entities[i].DeleteEntity();
            entities[i] = store.CreateEntity();
        }

        // 测试EntityBatch批量添加组件
        sw.Restart();
        for (int i = 0; i < entities.Length; i++)
        {
            entities[i].Batch()
                .Add(new TestComponent { value = i })
                .Add(new Position(i, i, i))
                .AddTag<TestTag>();
        }
        sw.Stop();
        long batchEntityBatchAddTime = sw.ElapsedMilliseconds;

        // 重置实体
        for (int i = 0; i < entities.Length; i++)
        {
            entities[i].DeleteEntity();
            entities[i] = store.CreateEntity();
        }

        // 测试BulkBatch批量添加组件
        sw.Restart();
        EntityBatch batch = new EntityBatch();
        batch.Add(new TestComponent { value = 0 }).Add(new Position()).AddTag<TestTag>();    //有个缺点，没办法支持Position(i,i,i)这种形式
        for (int i = 1; i < entities.Length; i++)
        {
            batch.ApplyTo(entities[i]);
        }
        sw.Stop();
        long bulkBatchAddTime = sw.ElapsedMilliseconds;

        // 重置实体
        for (int i = 0; i < entities.Length; i++)
        {
            entities[i].DeleteEntity();
            entities[i] = store.CreateEntity();
        }

        // 测试EntityList批量添加组件
        sw.Restart();
        EntityList list = new EntityList(store);
        for (int i = 1; i < entities.Length; i++)
        {
            list.Add(entities[i]);
        }
        EntityBatch batch2 = new EntityBatch();
        batch2.Add(new TestComponent { value = 0 }).Add(new Position()).AddTag<TestTag>();  //有个缺点，没办法支持Position(i,i,i)这种形式
        list.ApplyBatch(batch2);
        sw.Stop();
        long entityListBatchAddTime = sw.ElapsedMilliseconds;

        DebugMgr.LogInfo(() => $"Individual Add: {individualAddTime}ms");
        DebugMgr.LogInfo(() => $"Batch Add: {batchAddTime}ms");
        DebugMgr.LogInfo(() => $"EntityBatch Add: {batchEntityBatchAddTime}ms");
        DebugMgr.LogInfo(() => $"BulkBatch Add: {bulkBatchAddTime}ms");
        DebugMgr.LogInfo(() => $"EntityList Batch Add: {entityListBatchAddTime}ms");
    }

    // 测试查询性能：普通查询 vs ForEach vs Chunks

    struct TestEach : IEach<TestComponent, Position>
    {
        public int sum;
        public void Execute(ref TestComponent testComponent, ref Position position)
        {
            sum += testComponent.value;
        }
    }

    public static void TestQueryPerformance()
    {
        DebugMgr.LogInfo(() => "=== Query Performance Test ===");

        // ParallelJobRunner runner = new ParallelJobRunner(Environment.ProcessorCount);

        EntityStore store = new EntityStore();
        // EntityStore store = new EntityStore() { JobRunner = runner };
        // 创建测试实体
        for (int i = 0; i < ENTITY_COUNT; i++)
        {
            store.CreateEntity(new TestComponent { value = i }, new Position(i, i, i));
        }

        ArchetypeQuery<TestComponent, Position> query = store.Query<TestComponent, Position>();

        // 测试普通迭代
        var sw = System.Diagnostics.Stopwatch.StartNew();
        int sum1 = 0;
        foreach (Entity entity in query.Entities)
        {
            sum1 += entity.GetComponent<TestComponent>().value;
        }
        sw.Stop();
        long normalIterationTime = sw.ElapsedMilliseconds;

        // 测试ForEachEntity
        sw.Restart();
        int sum2 = 0;
        query.ForEachEntity((ref TestComponent comp, ref Position pos, Entity entity) =>
        {
            sum2 += comp.value;
        });
        sw.Stop();
        long forEachTime = sw.ElapsedMilliseconds;

        // Each
        sw.Restart();
        TestEach testEach = new TestEach() { sum = 0 };
        query.Each(testEach);
        int sum3 = testEach.sum;
        sw.Stop();
        long eachTime = sw.ElapsedMilliseconds;

        // 测试Chunks迭代
        sw.Restart();
        int sum4 = 0;
        foreach (var (testComps, positions, entities) in query.Chunks)
        {
            for (int i = 0; i < entities.Length; i++)
            {
                sum4 += testComps[i].value;
            }
        }
        sw.Stop();
        long chunksTime = sw.ElapsedMilliseconds;

        // // 测试并行查询
        // sw.Restart();
        // int sum5 = 0;
        // query.ForEach((testComponents, positions, entities) =>
        // {
        //     for (int i = 0; i < entities.Length; i++)
        //     {
        //         sum5 += testComponents[i].value;
        //     }
        // }).RunParallel();
        // sw.Stop();
        // long parallelTime = sw.ElapsedMilliseconds;

        // runner.Dispose();

        DebugMgr.LogInfo(() => $"Normal Iteration: {normalIterationTime}ms (sum: {sum1})");
        DebugMgr.LogInfo(() => $"ForEachEntity: {forEachTime}ms (sum: {sum2})");
        DebugMgr.LogInfo(() => $"Each: {eachTime}ms (sum: {sum3})");
        DebugMgr.LogInfo(() => $"Chunks Iteration: {chunksTime}ms (sum: {sum4})");
        // DebugMgr.LogInfo(() => $"Parallel Query: {parallelTime}ms (sum: {sum5})");
    }

    // 测试并行查询性能
    public static void TestParallelQueryPerformance()
    {
        DebugMgr.LogInfo(() => "=== Parallel Query Performance Test ===");
        
        ParallelJobRunner runner = new ParallelJobRunner(Environment.ProcessorCount);
        EntityStore store = new EntityStore { JobRunner = runner };
        
        // 创建大量实体进行测试
        for (int i = 0; i < ENTITY_COUNT; i++)
        {
            store.CreateEntity(new TestComponent { value = i });
        }
        
        ArchetypeQuery<TestComponent> query = store.Query<TestComponent>();
        
        // 测试单线程查询
        var sw = System.Diagnostics.Stopwatch.StartNew();
        query.ForEachEntity((ref TestComponent comp, Entity entity) =>
        {
            comp.value += 10; // 简单的计算操作
        });
        sw.Stop();
        long singleThreadTime = sw.ElapsedMilliseconds;
        
        // 重置数据
        query.ForEachEntity((ref TestComponent comp, Entity entity) =>
        {
            comp.value -= 10;
        });
        
        // 测试并行查询
        sw.Restart();
        QueryJob<TestComponent> queryJob = query.ForEach((testComponents, entities) =>
        {
            for (int i = 0; i < entities.Length; i++)
            {
                testComponents[i].value += 10;
            }
        });
        queryJob.RunParallel();     //wht que 为什么比单线程查询慢？
                                    //      https://github.com/friflo/friflo-ecs-unity/issues/12
        sw.Stop();
        long parallelTime = sw.ElapsedMilliseconds;
        
        DebugMgr.LogInfo(() => $"Single Thread: {singleThreadTime}ms");
        DebugMgr.LogInfo(() => $"Parallel ({Environment.ProcessorCount} cores): {parallelTime}ms");
        
        runner.Dispose();
    }

    // 测试索引查询性能
    public static void TestIndexedQueryPerformance()
    {
        DebugMgr.LogInfo(() => "=== Indexed Query Performance Test ===");
        
        EntityStore store = new EntityStore();
        ComponentIndex<IndexedTestComponent, int> index = store.ComponentIndex<IndexedTestComponent, int>();
        
        // 创建测试数据
        const int uniqueIds = 1000;
        for (int i = 0; i < ENTITY_COUNT; i++)
        {
            store.CreateEntity(new IndexedTestComponent { id = i % uniqueIds, value = i });
        }
        
        // 测试普通查询性能
        var sw = System.Diagnostics.Stopwatch.StartNew();
        int foundCount1 = 0;
        int targetId = uniqueIds / 2;
        ArchetypeQuery<IndexedTestComponent> query = store.Query<IndexedTestComponent>();
        query.ForEachEntity((ref IndexedTestComponent comp, Entity entity) =>
        {
            if (comp.id == targetId)
                foundCount1++;
        });
        sw.Stop();
        long normalQueryTime = sw.ElapsedMilliseconds;
        
        // 测试索引查询性能
        sw.Restart();
        Entities entities = index[targetId];
        int foundCount2 = entities.Count;
        sw.Stop();
        long indexedQueryTime = sw.ElapsedMilliseconds;
        
        DebugMgr.LogInfo(() => $"Normal Query: {normalQueryTime}ms (found: {foundCount1})");
        DebugMgr.LogInfo(() => $"Indexed Query: {indexedQueryTime}ms (found: {foundCount2})");
    }

    // 测试组件访问性能：单个访问 vs 批量访问
    public static void TestComponentAccessPerformance()
    {
        DebugMgr.LogInfo(() => "=== Component Access Performance Test ===");
        
        EntityStore store = new EntityStore();
        var entities = new Entity[ENTITY_COUNT];
        
        // 创建测试实体
        for (int i = 0; i < ENTITY_COUNT; i++)
        {
            entities[i] = store.CreateEntity(
                new TestComponent { value = i },
                new Position(i, i, i),
                new Scale3(i, i, i));
        }
        
        // 测试逐个访问组件
        var sw = System.Diagnostics.Stopwatch.StartNew();
        int sum1 = 0;
        for (int i = 0; i < entities.Length; i++)
        {
            var comp = entities[i].GetComponent<TestComponent>();
            var pos = entities[i].GetComponent<Position>();
            var scale = entities[i].GetComponent<Scale3>();
            sum1 += comp.value;
        }
        sw.Stop();
        long individualAccessTime = sw.ElapsedMilliseconds;
        
        // 测试使用entity.Data批量访问
        sw.Restart();
        int sum2 = 0;
        for (int i = 0; i < entities.Length; i++)
        {
            var data = entities[i].Data;            //wht que 为什么更慢？https://github.com/friflo/friflo-ecs-unity/issues/13
            var comp = data.Get<TestComponent>();
            var pos = data.Get<Position>();
            var scale = data.Get<Scale3>();
            sum2 += comp.value;
        }
        sw.Stop();
        long batchAccessTime = sw.ElapsedMilliseconds;
        
        DebugMgr.LogInfo(() => $"Individual Access: {individualAccessTime}ms (sum: {sum1})");
        DebugMgr.LogInfo(() => $"Batch Access (entity.Data): {batchAccessTime}ms (sum: {sum2})");
    }

    // 综合性能测试
    public static void RunAllPerformanceTests()
    {
        DebugMgr.LogInfo(() => "========================================");
        DebugMgr.LogInfo(() => "    Friflo ECS Performance Tests");
        DebugMgr.LogInfo(() => $"    Large Entity Count: {ENTITY_COUNT:N0}");
        DebugMgr.LogInfo(() => $"    Processor Count: {Environment.ProcessorCount}");
        DebugMgr.LogInfo(() => "========================================");
        
        TestEntityCreationPerformance();
        
        TestBatchOperationsPerformance();
        
        TestQueryPerformance();
        
        TestParallelQueryPerformance();
        
        TestIndexedQueryPerformance();
        
        TestComponentAccessPerformance();
        
        DebugMgr.LogInfo(() => "========================================");
        DebugMgr.LogInfo(() => "    Performance Tests Completed");
        DebugMgr.LogInfo(() => "========================================");
    }
}