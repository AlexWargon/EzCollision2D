using System.Collections.Generic;

using UnityEngine;
using Wargon.ezs;
using Wargon.ezs.Unity;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace Wargon.EzCollision2D.Example {
    public class GizsomHelper : MonoBehaviour {
    public bool render;
    [SerializeField] private Color green;
    [SerializeField] private Color red;
    private GizsomDrawer drawer;
    private void Start()
    {
        drawer = new GizsomDrawer();
        GizsomDrawer.Instance = drawer;
        drawer.AddRender(new Colliders2DRenders(green, red));
    }
    private void OnDrawGizmos()
    {
        if(!render) return;
        drawer?.Draw();
        if (Grid2D.Instance != null) {
            var grid = Grid2D.Instance;
            grid.DrawCells();
        }
    }
    
    public interface IGizmosRender {
        void Render();
    }
    public class GizsomDrawer {
        private readonly List<IGizmosRender> gizmosList = new();
        public static GizsomDrawer Instance;
        public void AddRender(IGizmosRender gizmosRender) {
            gizmosList.Add(gizmosRender);
        }
        public void RemoveRender(IGizmosRender gizmosRender) {
            gizmosList.Remove(gizmosRender);
        }
        public void Draw()
        {
#if UNITY_EDITOR
            for (int i = 0; i < gizmosList.Count; i++) {
                gizmosList[i].Render();
            }
#endif
        }
    }

    private class Colliders2DRenders : IGizmosRender {
        private EntityQuery query2;
        private EntityQuery query;
        private Pool<Circle2D> circles;
        private Pool<Rectangle2D> rectangles;
        private Pool<TransformComponent> transforms;
        private Color green;
        private Color red;
        public void Render() {
#if UNITY_EDITOR
            for (int i = 0; i < query.Count; i++)
            {
                var entity = query.GetEntityIndex(i);
                ref var c = ref circles.items[entity];
                Handles.color = c.collided ? green : red;
                Handles.DrawWireDisc(new Vector3(c.position.x,c.position.y,0), Vector3.forward, c.radius, 3);
            }
            for (int i = 0; i < query2.Count; i++)
            {
                var entity = query2.GetEntityIndex(i);
                ref var c = ref rectangles.items[entity];
                ref var transform = ref transforms.items[entity];
                Vector3 Y1 = new Vector3(transform.position.x, transform.position.y + c.h);
                Vector3 X1 = new Vector3(transform.position.x, transform.position.y);
                Vector3 Y2 = new Vector3(transform.position.x + c.w, transform.position.y + c.h);
                Vector3 X2 = new Vector3(transform.position.x + c.w, transform.position.y);
                Debug.DrawLine(X1, Y1, green);
                Debug.DrawLine(Y1, Y2, green);
                Debug.DrawLine(Y2, X2, green);
                Debug.DrawLine(X2, X1, green);
            }
#endif
        }
        public Colliders2DRenders(Color g, Color r) {
            var world = MonoConverter.GetWorld();
            query2 = world.GetQuery().With<Rectangle2D>().Without<Inactive>();
            query = world.GetQuery().With<Circle2D>().Without<Inactive>();
            circles = world.GetPool<Circle2D>();
            rectangles = world.GetPool<Rectangle2D>();
            transforms = world.GetPool<TransformComponent>();
            green = g;
            red = r;
        }
    }
}
}

