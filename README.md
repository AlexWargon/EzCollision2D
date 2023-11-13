# EzCollision2D

2D Collision based on [EZS](https://github.com/AlexWargon/EZS) using jobs and burst. But it can be easy ported on other frameworks.

####  Include:
[Shapes]: 
Circle, Rect.
[Casts]:
CircleOverlap
#### Collision Query

```c#


NativeQueue<HitInfo> hits = Grid2D.Instance.Hits;

while (hits.Count > 0) {
    var hit = hits.Dequeue();
}

```
