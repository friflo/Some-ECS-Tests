using Friflo.Engine.ECS;

namespace RouderSky;

public static class TraverseEntities
{
    struct LifeCycleCom : IComponent;
    
    public  static void Run()
    {
        var entityStore = new EntityStore();
        entityStore.CreateEntity(new LifeCycleCom());
        var allEnts = entityStore.Entities;
        for (int j = 0; j < allEnts.Count; j++)
        {
            // Note!  ElementAt(j) is a Linq extension method. Each call will iterate IEnumerable <Entity>  =>  O(N)
            var curEnt = allEnts.ElementAt(j);    //Is there a way to directly get the Entity from EntityStore via reference instead of causing a struct copy?
            ProcessOneEnt(ref curEnt.GetComponent<LifeCycleCom>(), ref curEnt, true);
        }
    }
    
    public  static void Run2()
    {
        var entityStore = new EntityStore();
        entityStore.CreateEntity(new LifeCycleCom());
        var query = entityStore.Query<LifeCycleCom>();
        query.ForEachEntity((ref LifeCycleCom lifeCycleCom, Entity entity) => {
            ProcessOneEnt(ref lifeCycleCom, ref entity, true);
        });
    }
    
    private static void ProcessOneEnt(ref LifeCycleCom lifeCycleCom, ref Entity entity, bool boolValue) {
    }
    
}