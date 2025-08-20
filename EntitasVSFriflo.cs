using Friflo.Engine.ECS;

namespace RouderSky;

public class EntitasVSFriflo
{
    public struct FrifloTestCom : IComponent;
    
    public static void Run()
    {
        ulong entityCount = 100_000;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        /* World entitasWorld = new World(
            (contexts) => new List<IExecuteSystem> {  },
            (contexts) => new List<IExecuteSystem> { new TestUpdateSys(contexts) },
            (contexts) => new List<IExecuteSystem> {  }
        );
        
        sw.Restart();            
        for (ulong n = 0; n < entityCount; n++)
        {
            GameEntity ent = entitasWorld.contexts.game.CreateEntity();
            ent.AddTestCom();
        }
        sw.Stop();
        DebugMgr.LogInfo(() => $"Entitas Create {entityCount} entities: {sw.ElapsedMilliseconds} ms");

        sw.Restart();
        foreach (var system in entitasWorld.updateSystems)
        {
            system.Execute();
        }
        sw.Stop();
        DebugMgr.LogInfo(() => $"Entitas Execute {entitasWorld.updateSystems.Count} systems: {sw.ElapsedMilliseconds} ms");
        */        
        EntityStore frifloWorld = new EntityStore();
        
        sw.Restart();

        for (ulong n = 0; n < entityCount; n++)
        {
            // Entity entity = frifloWorld.CreateEntity(new FrifloTestCom());
            Entity entity = frifloWorld.CreateEntity();
            entity.AddComponent(new FrifloTestCom());
        }
        sw.Stop();
        DebugMgr.LogInfo(() => $"Friflo Create {entityCount} entities: {sw.Elapsed.TotalMilliseconds} ms");
        
        // chunks查询
        sw.Restart();
        ArchetypeQuery<EntityName> query = frifloWorld.Query<EntityName>();
        foreach (var (components, entities) in query.Chunks)
        {
            for (int n = 0; n < entities.Length; n++)
            {
                
            }
        }
        sw.Stop();
        DebugMgr.LogInfo(() => $"Friflo Query {query.Count} entities: {sw.Elapsed.TotalMilliseconds} ms");
    }
}