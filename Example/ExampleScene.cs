using UnityEngine;
using Wargon.DI;
using Wargon.ezs;
using Wargon.ezs.Unity;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Wargon.EzCollision2D.Example {
    [DefaultExecutionOrder(-35)]
    public class ExampleScene : MonoBehaviour {
        [SerializeField] private MonoEntity circle;
        private World world;
        private Systems systems;
        private void Awake() {

            world = new World();
            MonoConverter.Init(world);
            Grid2D grid2D = new Grid2D(10, 6,6,Vector2.zero,world);
            Injector.AddAsSingle(grid2D);
            systems = new Systems(world)
                    .Add(new SpawnCircleSystem(circle))
                    .Add(new Collision2DGroup())
                    .Add(new SyncTransformSystem())
                    .Add(new RemoveComponentSystem(typeof(EntityConvertedEvent)))
                ;
            systems.Init();

#if UNITY_EDITOR
            var _ = new DebugInfo(world);
#endif
        }

        private void Update() {
            systems.OnUpdate();
        }

        private void OnDestroy() {
            if (world != null) {
                world.Destroy();
                systems = null;
                world = null;
            }
            Grid2D.Instance.Clear();
        }
    }

    public partial class SpawnCircleSystem : UpdateSystem {
        private MonoEntity circle;
        public SpawnCircleSystem(MonoEntity circle) {
            this.circle = circle;
        }
        public override void Update() {
            if (Input.GetKey(KeyCode.Space)) {
                var pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                pos.z = 0;
                pos.x += Random.Range(-0.1f, 0.1f);
                pos.y += Random.Range(-0.1f, 0.1f);
                Object.Instantiate(circle, pos, Quaternion.identity);
            }
            
            if (Input.GetKey(KeyCode.R)) {
                var pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                pos.z = 0;
                var cached = Cast.CircleOverlap(pos, 0.7F, out int count);
                for (int i = 0; i < count; i++) {
                    var hit = cached[i];
                    var e = world.GetEntity(hit.Index);
                    if (!e.IsNULL()) {
                        Object.Destroy(e.Get<TransformRef>().value.gameObject);
                        e.Destroy();
                    }
                }
                cached.Dispose();
            }
        }
    }
    public partial class SyncTransformSystem : UpdateSystem {
        private Pool<TransformRef> transforms;
        private Pool<TransformComponent> transformPure;
        private EntityQuery query;
        protected override void OnCreate() {
            query = world.GetQuery().With<TransformRef>().With<TransformComponent>().Without<Inactive>().Without<StaticTag>();
            transforms = world.GetPool<TransformRef>();
            transformPure = world.GetPool<TransformComponent>();
        }

        public override void Update() {
            //Span<TransformComponent> spanT = new Span<TransformComponent>(transformPure.items);
            //Span<TransformRef> spanR = new Span<TransformRef>(transforms.items);
            
            for (var i = 0; i < query.Count; i++) {
                var index = query.GetEntityIndex(i);
                ref var transformRef = ref transforms.items[index];
                ref var transformComponent = ref transformPure.items[index];
                // transformComponent.right = transformComponent.rotation * UnityEngine.Vector3.right;
                // transformComponent.forward = transformComponent.rotation * UnityEngine.Vector3.forward;
                transformRef.value.position = transformComponent.position;
                transformRef.value.rotation = transformComponent.rotation;
                transformRef.value.localScale = transformComponent.scale;
            }
        }
    }
}

