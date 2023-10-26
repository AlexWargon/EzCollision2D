using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using Wargon.ezs;
using Wargon.ezs.Unity;

namespace Wargon.EzCollision2D {
    
    [EcsComponent]
    public struct Circle2D : IEquatable<Circle2D> {
        public int index;
        public int cellIndex;
        public float radius;
        public float2 position;
        public bool collided;
        public bool trigger;
        public CollisionLayer layer;
        public CollisionLayer collideWith;
        public override int GetHashCode() {
            return index;
        }

        public bool Equals(Circle2D other) {
            return other.index == index;
        }
    }

    [EcsComponent]
    public struct Rectangle2D {
        public int index;
        public float w;
        public float h;
        public CollisionLayer layer;
        public CollisionLayer collisionWith;
    }
    public unsafe struct BufferInt32 {
        private fixed int buffer[32];
        private int count;

        public void Add(int value) {
            buffer[count++] = value;
        }

        public void Clear() {
            count = 0;
        }
    }
    public unsafe struct BufferInt9 {
        private fixed int buffer[9];
        private int count;
        public int Count {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => count;
            [MethodImpl(MethodImplOptions.AggressiveInlining)] set => count = value;
        }

        public int this[int index] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => buffer[index];
            [MethodImpl(MethodImplOptions.AggressiveInlining)] set => buffer[index] = value;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(int value) {
            if (count == 8) return;
            buffer[count++] = value;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() {
            count = 0;
        }
    }
    public unsafe struct BufferInt64 {
        private fixed int buffer[64];
        private int count;

        public void Add(int value) {
            buffer[count++] = value;
        }

        public void Clear() {
            count = 0;
        }
    }

    public unsafe struct BufferInt128 {
        private fixed int buffer[128];
        private int count;
        public int Count {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => count;
            [MethodImpl(MethodImplOptions.AggressiveInlining)] set => count = value;
        }

        public int this[int index] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => buffer[index];
            [MethodImpl(MethodImplOptions.AggressiveInlining)] set => buffer[index] = value;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(int value) {
            if (count == 127) return;
            buffer[count++] = value;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() {
            count = 0;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct BufferInt256 {
        private fixed int buffer[256];
        private int count;
        public int Count {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => count;
            [MethodImpl(MethodImplOptions.AggressiveInlining)] set => count = value;
        }

        public int this[int index] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => buffer[index];
            [MethodImpl(MethodImplOptions.AggressiveInlining)] set => buffer[index] = value;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(int value) {
            if (count == 255) return;
            buffer[count++] = value;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() {
            count = 0;
        }
    }

    public unsafe struct BufferInt512 {
        private fixed int buffer[512];
        private int count;

        public int Count {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => count;
            [MethodImpl(MethodImplOptions.AggressiveInlining)] set => count = value;
        }
        public int this[int index] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => buffer[index];
            [MethodImpl(MethodImplOptions.AggressiveInlining)] set => buffer[index] = value;
        }
        public void Add(int value) {
            if (count == 511) return;
            buffer[count++] = value;
        }

        public void Clear() {
            count = 0;
        }
    }

    public class Collision2DGroup : SystemsGroup {
        public Collision2DGroup() : base("CollisionSystems") {
            Add(new Collision2DOnConvertEntitySystem())
                .Add(new Collision2DOnRectangleSpawnSystem())
                .Add(new CollisionClearGridCellsSystem())
                .Add(new Collision2DPopulateRectSystem())
                .Add(new Collision2DPopulateSystem())
                .Add(new Collision2DSystem());
        }
    }
    [SystemColor("green")]
    partial class Collision2DPopulateRectSystem : UpdateSystem {
        [Inject] private Grid2D grid2D;
        private Pool<Rectangle2D> colliders;
        private Pool<TransformComponent> transforms;
        private EntityQuery query;
        protected override void OnCreate() {
            query = world.GetQuery().With<Rectangle2D>().With<TransformComponent>().Without<EntityConvertedEvent>().Without<Inactive>();
            colliders = world.GetPool<Rectangle2D>();
            transforms = world.GetPool<TransformComponent>();
        }
        public override void Update() {
            var nativeColliders = colliders.AsNative();
            var populateJob = new PopulateCellsJob {
                query = query.AsNative(),
                rectangles = nativeColliders,
                transforms = transforms.AsNative(),
                cells = grid2D.cells,
                cellSizeX = grid2D.CellSize,
                cellSizeY = grid2D.CellSize,
                Offset = grid2D.Offset,
                GridPosition = grid2D.Position,
                W = grid2D.W,
                H = grid2D.H
            };
            populateJob.Run(query.Count);
        }
    [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Fast)]
    public struct PopulateCellsJob : IJobFor {
        public NativeEntityQuery query;
        public UnsafeList<Grid2DCell> cells;
        public NativePool<TransformComponent> transforms;
        public NativePool<Rectangle2D> rectangles;
        public int cellSizeX, cellSizeY, W, H;
        public Vector2 Offset;
        public Vector2 GridPosition;

        public void Execute(int index) {
            var e = query.GetEntity(index);
            ref var rect = ref rectangles.Get(e);
            //circle.collided = false;
            ref var transform = ref transforms.Get(e);
            //circle.position = new float2(transform.position.x, transform.position.y);
            var px = Mathf.FloorToInt((transform.position.x - Offset.x - GridPosition.x) / cellSizeX);
            var py = Mathf.FloorToInt((transform.position.y - Offset.y - GridPosition.y) / cellSizeY);
            var cellIndex = py * W + px;
            if (cellIndex > -1 && cellIndex < cells.Length) {
                var cell = cells[cellIndex];
                cell.RectanglesBuffer.Add(rect.index);
                cells[cellIndex] = cell;
            }
        }
    }
}
    [SystemColor("green")]
    partial class Collision2DOnConvertEntitySystem : UpdateSystem {
        public override void Update() {
            entities.Each((Entity e, Circle2D circle, TransformRef transform, TransformComponent pureTransform, EntityConvertedEvent convertedEvent) => {
                pureTransform.position = transform.value.position;
                circle.position = new float2(pureTransform.position.x, pureTransform.position.y);
                circle.index = e.id;
                circle.cellIndex = -1;
                circle.collided = false;
            });
        }
    }
    [SystemColor("green")]
    partial class Collision2DOnRectangleSpawnSystem : UpdateSystem {
        [Inject] private Grid2D grid2D;
        public override void Update() {
            entities.Each((Entity Entity, Rectangle2D rect, TransformComponent transformComponent,
                TransformRef transformRef, EntityConvertedEvent convertedEvent) => {
                transformComponent.position = transformRef.value.position;
                rect.index = Entity.id;
                Entity.Add(new StaticTag());
            });
        }
    }

    [SystemColor("green")]
    partial class Collision2DPopulateSystem : UpdateSystem {
        [Inject] private Grid2D grid2D;
        private Pool<Circle2D> colliders;
        private Pool<TransformComponent> transforms;
        private EntityQuery query;
        private JobHandle jobHandle;
        protected override void OnCreate() {
            query = world.GetQuery().With<TransformRef>().With<Circle2D>().With<TransformComponent>().Without<EntityConvertedEvent>().Without<Inactive>();
            colliders = world.GetPool<Circle2D>();
            transforms = world.GetPool<TransformComponent>();
        }
        public override void Update() {

            var populateJob = new PopulateCellsJob {
                query = query.AsNative(),
                colliders = colliders.AsNative(),
                transforms = transforms.AsNative(),
                cells = grid2D.cells,
                cellSizeX = grid2D.CellSize,
                cellSizeY = grid2D.CellSize,
                Offset = grid2D.Offset,
                GridPosition = grid2D.Position,
                W = grid2D.W,
                H = grid2D.H
            };
            populateJob.Run(query.Count);
        }
    }
    [SystemColor("green")]
    partial class Collision2DSystem : UpdateSystem {
        [Inject] private Grid2D grid2D;
        private Pool<Circle2D> colliders;
        private Pool<Rectangle2D> rectnagles;
        private Pool<TransformComponent> transforms;
        private JobHandle jobHandle;
        protected override void OnCreate() {
            colliders = world.GetPool<Circle2D>();
            transforms = world.GetPool<TransformComponent>();
            rectnagles = world.GetPool<Rectangle2D>();
        }
        public override void Update() {
            grid2D.Hits.Clear();
            var writer = grid2D.Hits.AsParallelWriter();
            var job = new Collision2DMark2ParallelHitsJob {
                hits = writer,
                cells = grid2D.cells,
                colliders = colliders.AsNative(),
                transforms = transforms.AsNative(),
                w = grid2D.W,
                h = grid2D.H,
                Offset = grid2D.Offset,
                GridPosition = grid2D.Position,
                cellSize = grid2D.CellSize,
                rectangles = rectnagles.AsNative()
            };
            job.Schedule(grid2D.cells.Length, 12).Complete();
        }
    }

    [SystemColor("green")]
    partial class CollisionClearGridCellsSystem : UpdateSystem {
        [Inject] private Grid2D grid2D;

        public override void Update() {
            var clearJob = new ClearJob {
                cells = grid2D.cells
            };
            clearJob.Schedule(grid2D.cells.Length,default).Complete();
        }
        [BurstCompile]
        struct ClearJob : IJobFor {
            public UnsafeList<Grid2DCell> cells;

            public void Execute(int i) {
                var cell = cells[i];
                cell.CollidersBuffer.Clear();
                cell.RectanglesBuffer.Clear();
                cells[i] = cell;
            }
        }
    }

    [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Fast)]
    public unsafe struct AddCircleToCellsJob : IJobFor {
        public UnsafeList<Grid2DCell> cells;
        public NativeArray<int> collidedCells;
        [NativeDisableUnsafePtrRestriction] public Circle2D* castCircle;
        [NativeDisableUnsafePtrRestriction] public int* count;

        public void Execute(int index) {
            var cell = cells[index];

            if (CircleRectangleCollision(castCircle, in cell)) collidedCells[(*count)++] = cell.index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CircleRectangleCollision(Circle2D* circle, in Grid2DCell rect) {
            var closestX = math.max(rect.Pos.x, math.min(circle->position.x, rect.Pos.x + rect.W));
            var closestY = math.max(rect.Pos.y, math.min(circle->position.y, rect.Pos.y + rect.H));

            var distanceX = circle->position.x - closestX;
            var distanceY = circle->position.y - closestY;

            var distanceSquared = distanceX * distanceX + distanceY * distanceY;

            return distanceSquared <= circle->radius * circle->radius;
        }
    }

    [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Fast)]
    public unsafe struct CheckCircleCollisionJobArray : IJob {
        [NativeDisableUnsafePtrRestriction] public Circle2D* colliders;
        [NativeDisableUnsafePtrRestriction] public Circle2D* castCircle;
        [NativeDisableUnsafePtrRestriction] public int* hitsCount;
        [WriteOnly] public NativeArray<HitInfo> hits;
        public NativeArray<int> cellIndexArray;
        public UnsafeList<Grid2DCell> cells;
        public int cellsCount;

        public void Execute() {
            for (var i = 0; i < cellsCount; i++) {
                var cellIndex = cellIndexArray[i];
                var cell = cells[cellIndex];
                for (var index = 0; index < cell.CollidersBuffer.Count; index++) {
                    var i2 = cell.CollidersBuffer[index];
                    ref var circle2 = ref UnsafeUtility.ArrayElementAsRef<Circle2D>(colliders, i2);
                    if ((castCircle->collideWith & circle2.layer) == circle2.layer || castCircle->collideWith == CollisionLayer.None)
                        if (Grid2D.IsOverlap(castCircle, in circle2, out var distance)) {
                            //if(*hitsCount < hits.Length)
                            hits[*hitsCount] = ResolveCollisionInternal(castCircle, ref circle2);
                            (*hitsCount)++;
                        }
                }
            }
        }

        [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Fast)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private HitInfo ResolveCollisionInternal(Circle2D* circle1, ref Circle2D circle2) {
            var pos = circle2.position - circle1->position;
            var normal = math.normalize(pos);
            return new HitInfo {
                Pos = pos,
                Normal = normal,
                From = -1,
                Index = circle2.index
            };
        }
    }

    [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Fast)]
    public unsafe struct CheckCircleCollisionJobQueue : IJob {
        public UnsafeList<Grid2DCell> cells;
        [NativeDisableUnsafePtrRestriction] public Circle2D* colliders;
        [NativeDisableUnsafePtrRestriction] public Circle2D* castCircle;
        [WriteOnly] public NativeQueue<HitInfo> hits;
        public NativeArray<int> cellsArray;
        public int count;

        public void Execute() {
            for (var i = 0; i < count; i++) {
                var cellIndex = cellsArray[i];
                var cell = cells[cellIndex];
                for (var i1 = 0; i1 < cell.CollidersBuffer.Count; i1++) {
                    var index = cell.CollidersBuffer[i1];
                    ref var circle2 = ref colliders[index];
                    if (Grid2D.IsOverlap(castCircle, in circle2, out var distance))
                        hits.Enqueue(ResolveCollisionInternal(castCircle, ref circle2, distance));
                }
            }
        }

        [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Fast)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private HitInfo ResolveCollisionInternal(Circle2D* circle1, ref Circle2D circle2, float distance) {
            var pos = circle2.position - circle1->position;
            var normal = math.normalize(pos);
            var radii = circle1->radius + circle2.radius;
            var depth = radii - distance + 0.01f;
            circle1->position -= normal * depth * 0.5F;
            circle2.position += normal * depth * 0.5f;
            return new HitInfo {
                Pos = normal,
                Normal = normal,
                From = -1,
                Index = circle2.index
            };
        }
    }

    [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Fast)]
    public struct PopulateCellsJob : IJobParallelFor {
        public NativeEntityQuery query;
        public UnsafeList<Grid2DCell> cells;
        public NativePool<Circle2D> colliders;
        public NativePool<TransformComponent> transforms;
        public int cellSizeX, cellSizeY, W, H;
        public Vector2 Offset;
        public Vector2 GridPosition;

        public void Execute(int index) {
            var e = query.GetEntity(index);
            ref var circle = ref colliders.Get(e);
            circle.collided = false;
            ref var transform = ref transforms.Get(e);
            circle.position = new float2(transform.position.x, transform.position.y);
            var px = Mathf.FloorToInt((transform.position.x - Offset.x - GridPosition.x) / cellSizeX);
            var py = Mathf.FloorToInt((transform.position.y - Offset.y - GridPosition.y) / cellSizeY);
            if (px >= 0 && px < W && py >= 0 && py < H) {
                var cellIndex = py * W + px;
                if (cellIndex > -1 && cellIndex < cells.Length) {
                    var cell = cells[cellIndex];
                    cell.CollidersBuffer.Add(circle.index);
                    circle.cellIndex = cellIndex;
                    cells[cellIndex] = cell;
                }
            }

        }
    }

    public struct HitInfo {
        public float2 Pos;
        public float2 Normal;
        public int From;
        public int Index;
    }
    
    [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Fast)]
    public struct Collision2DMark2ParallelHitsJob : IJobParallelFor {
        public UnsafeList<Grid2DCell> cells;
        public NativePool<Circle2D> colliders;
        public NativePool<TransformComponent> transforms;
        public NativePool<Rectangle2D> rectangles;
        [WriteOnly] public NativeQueue<HitInfo>.ParallelWriter hits;
        public int w, h;
        public int cellSize;
        public Vector2 Offset, GridPosition;

        public void Execute(int idx) {
            var x = idx % w;
            var y = idx / w;

            var cell1 = cells[idx];
            cell1.Pos = new Vector2(x * cellSize, y * cellSize) + Offset + GridPosition;
            cells[idx] = cell1;

            for (var dx = -1; dx <= 1; ++dx)
            for (var dy = -1; dy <= 1; ++dy) {
                
                var di = w * (y + dy) + x + dx;
                if(di < 0 || di >= cells.Length) continue;
                var cell2 = cells[di];

                for (var i = 0; i < cell1.CollidersBuffer.Count; i++) {
                    var i1 = cell1.CollidersBuffer[i];
                    ref var circle1 = ref colliders.Get(i1);
                    ref var t1 = ref transforms.Get(circle1.index);
                    circle1.position = new float2(t1.position.x, t1.position.y);

                    for (var j = 0; j < cell2.CollidersBuffer.Count; j++) {
                        var i2 = cell2.CollidersBuffer[j];
                        if (i1 == i2) continue;
                        ref var circle2 = ref colliders.Get(i2);
                        if ((circle1.collideWith & circle2.layer) == circle2.layer)
                            if (Grid2D.IsOverlap(in circle1, in circle2, out var distance)) {
                                circle1.collided = true;
                                circle2.collided = true;
                                ref var t2 = ref transforms.Get(circle2.index);
                                if (circle1.layer == CollisionLayer.Enemy && circle2.layer == CollisionLayer.Enemy)
                                    ResolveCollisionInternal(ref circle1, ref circle2, distance, ref t1, ref t2);
                                else
                                    hits.Enqueue(ResolveCollisionInternal(ref circle1, ref circle2, distance, ref t1, ref t2));
                            }
                    }
                    
                    for (var j = 0; j < cell2.RectanglesBuffer.Count; j++) {
                        var i2 = cell2.RectanglesBuffer[j];
                        if (i1 == i2) continue;
                        ref var rect = ref rectangles.Get(i2);
                        ref var rectTransform = ref transforms.Get(i2);
                        if (CircleRectangleCollision(in circle1, in rect, in rectTransform)) {
                            circle1.collided = true;
                            //hits.Enqueue(ResolveCollisionCircleVsRectInternal(ref circle1, in rect, in rectTransform));
                            hits.Enqueue(ResolveCollisionCircleVsRectInternal(ref circle1, ref t1, in rect, in rectTransform));
                        }
                    }
                }
            }
        }

        [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Default)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        HitInfo ResolveCollisionCircleVsRectInternal(ref Circle2D circle, ref TransformComponent circleTransform, in Rectangle2D rect, in TransformComponent rectTransform)
        {
            float closestX = math.max(rectTransform.position.x, math.min(circle.position.x, rectTransform.position.x + rect.w));
            float closestY = math.max(rectTransform.position.y, math.min(circle.position.y, rectTransform.position.y + rect.h));

            float deltaX = circle.position.x - closestX;
            float deltaY = circle.position.y - closestY;

            float distance;
            if (deltaX == 0 && deltaY == 0)
            {
                distance = 0.0f;
            }
            else
            {
                distance = math.sqrt(deltaX * deltaX + deltaY * deltaY);
            }
            float overlap = circle.radius - distance;
            float2 normal = default;
            if (distance != 0) {
                normal = new float2(deltaX / distance, deltaY / distance);
            }

            circle.position += (normal * overlap);
                
            float collisionX = circle.position.x - normal.x * circle.radius;
            float collisionY = circle.position.y - normal.y * circle.radius;
            circleTransform.position.x = circle.position.x;
            circleTransform.position.y = circle.position.y;
            return new HitInfo {
                Pos = new float2(collisionX, collisionY),
                Normal = normal,
                From = circle.index,
                Index = -1
            };
        }
        [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Default)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool CircleRectangleCollision(in Circle2D circle, in Rectangle2D rectangle2D, in TransformComponent rectTransform)
        {
            // Find the closest point on the rectangle to the circle
            float closestX = math.max(rectTransform.position.x, math.min(circle.position.x, rectTransform.position.x + rectangle2D.w));
            float closestY = math.max(rectTransform.position.y, math.min(circle.position.y, rectTransform.position.y + rectangle2D.h));

            // Calculate the distance between the circle's center and the closest point
            float distanceX = circle.position.x - closestX;
            float distanceY = circle.position.y - closestY;
            float distanceSquared = (distanceX * distanceX) + (distanceY * distanceY);

            // Check if the distance is less than or equal to the circle's radius squared
            return distanceSquared <= (circle.radius * circle.radius);
        }
        [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Fast)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private HitInfo ResolveCollisionInternal(ref Circle2D circle1, ref Circle2D circle2, float distance,
            ref TransformComponent t1, ref TransformComponent t2) {
            var direction = circle2.position - circle1.position;
            var normal = math.normalize(direction);
            var depth = circle1.radius + circle2.radius - distance + 0.01f;
            if (!(circle1.trigger || circle2.trigger)) {
                circle1.position -= normal * depth * 0.5F;
                circle2.position += normal * depth * 0.5f;
                t1.position.x = circle1.position.x;
                t1.position.y = circle1.position.y;
                t2.position.x = circle2.position.x;
                t2.position.y = circle2.position.y;
            }

            return new HitInfo {
                Pos = circle1.position + normal * circle1.radius,
                Normal = normal,
                From = circle1.index,
                Index = circle2.index
            };
        }
    }
    [Flags]
    public enum CollisionLayer {
        None = 0,
        Player =  1 << 0,
        Enemy = 1 << 1
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct Grid2DCell {
        public int W;
        public int H;
        public int index;
        public float2 Pos;
        public BufferInt512 CollidersBuffer;
        public BufferInt128 RectanglesBuffer;
        private Vector3 Y1 => new Vector3(Pos.x, Pos.y + H);
        private Vector3 X1 => new Vector3(Pos.x, Pos.y);
        private Vector3 Y2 => new Vector3(Pos.x + W, Pos.y + H);
        private Vector3 X2 => new Vector3(Pos.x + W, Pos.y);
        public void Draw(Color color) {
            Debug.DrawLine(X1, Y1, color);
            Debug.DrawLine(Y1, Y2, color);
            Debug.DrawLine(Y2, X2, color);
            Debug.DrawLine(X2, X1, color);
        }
    }
    public class Grid2D {
        public static Grid2D Instance;
        public UnsafeList<Grid2DCell> cells;
        internal unsafe Circle2D* colliders;
        private int count;
        public NativeQueue<HitInfo> Hits;
        private int len;
        public Vector2 Offset;
        public Vector2 Position;
        public int W, H, CellSize;

        public Grid2D(int w, int h, int cellSize, Vector2 offset, World world, Vector2 position = default) {

            Position = position;
            W = w;
            H = h;
            Offset = offset;
            CellSize = cellSize;
            cells = new UnsafeList<Grid2DCell>(w * h, Allocator.Persistent);
            cells.Length = w * h;
            unsafe {
                fixed (Circle2D* ptr = world.GetPool<Circle2D>().items) {
                    colliders = ptr;
                }
            }

            Hits = new NativeQueue<HitInfo>(Allocator.Persistent);
            world.GetPool<Circle2D>().OnResize += OnPoolResize;

            for (var x = 0; x < w; x++)
            for (var y = 0; y < h; y++) {
                var i = w * y + x;
                
                var cell = new Grid2DCell {
                    W = cellSize,
                    H = cellSize,
                    Pos = new Vector2(x * cellSize, y * cellSize) + offset + Position,
                    CollidersBuffer = default,
                    RectanglesBuffer = default,
                    index = i
                };
                cells[i] = cell;
            }

            Instance = this;
        }


        public void Clear() {
            cells.Dispose();
            Hits.Dispose();
        }
        private unsafe void OnPoolResize(Pool<Circle2D> pool) {
            fixed (Circle2D* ptr = pool.items) {
                colliders = ptr;
            }
        }

        public void DrawCells() {
#if UNITY_EDITOR
            var style = new GUIStyle();
            style.normal.textColor = Color.white;

            for (var i = 0; i < cells.Length; i++) {
                var cell = cells[i];
                cell.Draw(Color.yellow);
                Handles.Label((Vector2)cell.Pos + Vector2.one, $"{cell.index}", style);
                Handles.Label((Vector2)cell.Pos + Vector2.one * 2, $"{cell.CollidersBuffer.Count}", style);
            }
#endif
        }

        [BurstCompile(FloatMode = FloatMode.Fast)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool IsOverlap(Circle2D* circle1, in Circle2D circle2, out float distance) {
            distance = math.distance(circle2.position, circle1->position);
            return circle1->radius + circle2.radius > distance;
        }

        [BurstCompile(FloatMode = FloatMode.Fast)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsOverlap(in Circle2D circle1, in Circle2D circle2, out float distance) {
            distance = math.distance(circle2.position, circle1.position);
            return circle1.radius + circle2.radius > distance;
        }
    }
}