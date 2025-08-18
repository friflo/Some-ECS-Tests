using Friflo.Engine.ECS;

namespace RouderSky;

public class TestBatchOperationsPerformance
{
    // 测试组件批量操作性能
    public static void Run()
    {
        IndividualAdd();
        BatchAdd();
        EntityBatchAdd();
        BulkBatchAdd();
        EntityListAdd();
    }
    
    private static EntityStore CreateStore(out Entity[] entities)
    {
        var count = TestFrifloECSPerformance.ENTITY_COUNT;
        EntityStore store = new EntityStore();
        entities = new Entity[count];
        for  (int i = 0; i < count; i++) {
            entities[i] = store.CreateEntity();
        }
        return store;
    }
    
    private static void IndividualAdd()
    {
        DebugMgr.LogInfo(() => "");
        DebugMgr.LogInfo(() => "=== Batch Operations Performance Test ===");

        EntityStore store = CreateStore(out Entity[] entities);

        // 测试逐个添加组件
        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < entities.Length; i++)
        {
            entities[i].AddComponent(new TestFrifloECSPerformance.TestComponent { value = i });
            entities[i].AddComponent(new Position(i, i, i));
            entities[i].AddTag<TestFrifloECSPerformance.TestTag>();
        }
        sw.Stop();
        long individualAddTime = sw.ElapsedMilliseconds;
        DebugMgr.LogInfo(() => $"Individual Add: {individualAddTime}ms");
    }

    private static void BatchAdd()
    {
        EntityStore store = CreateStore(out Entity[] entities);
        var tags = Tags.Get<TestFrifloECSPerformance.TestTag>();

        // 测试批量添加组件
        // wht que 为什么这里是最快的？
        //      https://github.com/friflo/friflo-ecs-unity/issues/11
        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < entities.Length; i++)
        {
            entities[i].Add(
                new TestFrifloECSPerformance.TestComponent { value = i },
                new Position(i, i, i),
                tags);
        }
        sw.Stop();
        long batchAddTime = sw.ElapsedMilliseconds;
        DebugMgr.LogInfo(() => $"Batch Add: {batchAddTime}ms");
    }

    private static void EntityBatchAdd()
    {
        EntityStore store = CreateStore(out Entity[] entities);

        // 测试EntityBatch批量添加组件
        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < entities.Length; i++)
        {
            entities[i].Batch()
                .Add(new TestFrifloECSPerformance.TestComponent { value = i })
                .Add(new Position(i, i, i))
                .AddTag<TestFrifloECSPerformance.TestTag>();
        }
        sw.Stop();
        long batchEntityBatchAddTime = sw.ElapsedMilliseconds;
        DebugMgr.LogInfo(() => $"EntityBatch Add: {batchEntityBatchAddTime}ms");
    }

    // * EntityBatch adds always components with same values.
    private static void BulkBatchAdd()
    {
        EntityStore store = CreateStore(out Entity[] entities);

        var sw = System.Diagnostics.Stopwatch.StartNew();

        EntityBatch batch = new EntityBatch();
        batch.Add(new TestFrifloECSPerformance.TestComponent { value = 0 }).Add(new Position()).AddTag<TestFrifloECSPerformance.TestTag>();    //有个缺点，没办法支持Position(i,i,i)这种形式
        for (int i = 1; i < entities.Length; i++)
        {
            batch.ApplyTo(entities[i]);
        }
        sw.Stop();
        long bulkBatchAddTime = sw.ElapsedMilliseconds;
        DebugMgr.LogInfo(() => $"BulkBatch Add: {bulkBatchAddTime}ms (*)");
    }

    // * EntityBatch adds always components with same values.
    private static void EntityListAdd()
    {
        EntityStore store = CreateStore(out Entity[] entities);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        EntityList list = new EntityList(store);
        for (int i = 1; i < entities.Length; i++)
        {
            list.Add(entities[i]);
        }
        EntityBatch batch2 = new EntityBatch();
        batch2.Add(new TestFrifloECSPerformance.TestComponent { value = 0 }).Add(new Position()).AddTag<TestFrifloECSPerformance.TestTag>();  //有个缺点，没办法支持Position(i,i,i)这种形式
        list.ApplyBatch(batch2);
        sw.Stop();
        long entityListBatchAddTime = sw.ElapsedMilliseconds;
        DebugMgr.LogInfo(() => $"EntityList Batch Add: {entityListBatchAddTime}ms (*)");
    }
}