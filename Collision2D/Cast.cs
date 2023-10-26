using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Wargon.EzCollision2D {
    public static class Cast {
        
        [BurstCompile]
        public static NativeQueue<HitInfo> CircleOverlap(Vector2 position, float radius) {
            unsafe {
                var circle = new Circle2D {
                    position = new float2(position.x, position.y), radius = radius, index = -900
                };
                var cells = new NativeArray<int>(30, Allocator.TempJob);
                var ptr = &circle;
                var count = 0;
                var countPtr = &count;
                var populateJob = new AddCircleToCellsJob {
                    castCircle = ptr,
                    cells = Grid2D.Instance.cells,
                    collidedCells = cells,
                    count = countPtr
                };
                populateJob.Run(Grid2D.Instance.cells.Length);


                var cache = new NativeQueue<HitInfo>(Allocator.TempJob);
                var collisionJob = new CheckCircleCollisionJobQueue {
                    castCircle = populateJob.castCircle,
                    cells = Grid2D.Instance.cells,
                    colliders = Grid2D.Instance.colliders,
                    hits = cache,
                    cellsArray = populateJob.collidedCells,
                    count = count
                };

                collisionJob.Run();
                cells.Dispose();
                return collisionJob.hits;
            }
        }
        [BurstCompile]
        public static NativeArray<HitInfo> CircleOverlap(Vector2 position, float radius, out int hitsCount, CollisionLayer collisionLayer) {
            unsafe {
                var circle = new Circle2D {
                    position = new float2(position.x, position.y),
                    radius = radius,
                    index = -900,
                    collideWith = collisionLayer
                };

                var cells = new NativeArray<int>(30, Allocator.TempJob);
                Circle2D* ptr = &circle;
                var cellsCount = 0;
                var cellCountPtr = &cellsCount;

                var populateJob = new AddCircleToCellsJob {
                    castCircle = ptr,
                    cells = Grid2D.Instance.cells,
                    collidedCells = cells,
                    count = cellCountPtr
                };
                populateJob.Run(Grid2D.Instance.cells.Length);

                var hitsCountInternal = 0;
                var hitsCountPTr = &hitsCountInternal;

                var cache = new NativeArray<HitInfo>(500, Allocator.TempJob);
                var collisionJob = new CheckCircleCollisionJobArray {
                    castCircle = populateJob.castCircle,
                    cells = Grid2D.Instance.cells,
                    colliders = Grid2D.Instance.colliders,
                    hits = cache,
                    cellIndexArray = populateJob.collidedCells,
                    cellsCount = cellsCount,
                    hitsCount = hitsCountPTr
                };
                collisionJob.Run();
                cells.Dispose();
                hitsCount = hitsCountInternal;
                return cache;
            }
        }
        [BurstCompile]
        public static NativeArray<HitInfo> CircleOverlap(Vector2 position, float radius, out int hitsCount) {
            unsafe {
                var circle = new Circle2D {
                    position = new float2(position.x, position.y),
                    radius = radius,
                    index = -900,
                    collideWith = CollisionLayer.None
                };

                var cells = new NativeArray<int>(30, Allocator.TempJob);
                Circle2D* ptr = &circle;
                var cellsCount = 0;
                var cellCountPtr = &cellsCount;

                var populateJob = new AddCircleToCellsJob {
                    castCircle = ptr,
                    cells = Grid2D.Instance.cells,
                    collidedCells = cells,
                    count = cellCountPtr
                };
                populateJob.Run(Grid2D.Instance.cells.Length);

                var hitsCountInternal = 0;
                var hitsCountPTr = &hitsCountInternal;

                var cache = new NativeArray<HitInfo>(500, Allocator.TempJob);
                var collisionJob = new CheckCircleCollisionJobArray {
                    castCircle = populateJob.castCircle,
                    cells = Grid2D.Instance.cells,
                    colliders = Grid2D.Instance.colliders,
                    hits = cache,
                    cellIndexArray = populateJob.collidedCells,
                    cellsCount = cellsCount,
                    hitsCount = hitsCountPTr
                };
                collisionJob.Run();
                cells.Dispose();
                hitsCount = hitsCountInternal;
                return cache;
            }
        }
    }
}