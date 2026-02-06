namespace Chess3d;

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;
using Stride.Physics;
using Stride.Rendering;
using Chess3d.ChessLogic;

public class BoardManager : SyncScript
{
    private readonly Entity[,] Markers = new Entity[8, 8];
    private readonly Entity[,] PiecesCache = new Entity[8, 8];
    private readonly Entity?[,] PiecesOnBoard = new Entity?[8, 8];
    private readonly MovesLogic MovesLogic = new();
    private Entity? SelectedPiece;
    private PiecePosition SelectedMarker;
    private static BoardManager Instance = null!;

    public CameraComponent Camera = null!;

    public override void Start()
    {
        base.Start();
        Instance = this;

        InitBoard();
    }

    public override void Update()
    {
        TrySelectPiece();

        DragPiece();

        DropPiece();
    }

    public static BoardManager GetInstance() => Instance;

    public void InitBoard()
    {
        static (float Width, float Height) ComputeModelBox(Model model)
        {
            var minX = float.MaxValue;
            var maxX = float.MinValue;
            var minY = float.MaxValue;
            var maxY = float.MinValue;

            foreach (var mesh in model.Meshes)
            {
                var bounds = mesh.BoundingBox;

                minX = MathF.Min(minX, bounds.Minimum.X);
                maxX = MathF.Max(maxX, bounds.Maximum.X);
                minY = MathF.Min(minY, bounds.Minimum.Y);
                maxY = MathF.Max(maxY, bounds.Maximum.Y);
            }

            return new(maxX - minX, maxY - minY);
        }

        Entity CreatePiece(string modelName, int my, int mx, string name, Dictionary<string, Model> modelCache)
        {
            if (!modelCache.TryGetValue(modelName, out var model))
            {
                model = Content.Load<Model>("Chess/" + modelName);
                modelCache[modelName] = model;
            }

            var entity = PiecesCache[my, mx];
            if (entity == null)
            {
                entity = new Entity { Name = name };
                entity.Add(new ModelComponent { Model = model });
                PiecesCache[my, mx] = entity;

                if (modelName == "Knight02")
                {
                    entity.Transform.Rotation = Quaternion.RotationY(MathUtil.DegreesToRadians(-90));
                }
                else if (modelName == "Knight01")
                {
                    entity.Transform.Rotation = Quaternion.RotationY(MathUtil.DegreesToRadians(90));
                }

                var (width, height) = ComputeModelBox(model);
                
                var radius = width * entity.Transform.Scale.X * 0.5f;
                var colliderHeight = height * entity.Transform.Scale.Y;

                var collider = new CylinderCollider { Radius = radius, Length = colliderHeight };
                var compoundCollider = new CompoundCollider();
                compoundCollider.Colliders.Add(collider);
                var component = new BodyComponent { Collider = compoundCollider, Kinematic = true, CollisionLayer = CollisionLayer.Layer1 };
                entity.Add(component);
            }

            entity.Transform.Position = Markers[my, mx].Transform.Position;

            PiecesOnBoard[my, mx] = entity;
            Entity.Scene.Entities.Add(entity);
            return entity;
        }

        #region Init Markers

        if (Markers[0, 0] == null)
        {
            var x = 0;
            var y = 0;
            foreach (var child in Entity.Scene.Entities.FirstOrDefault(x => x.Name == "BoardMarkers").GetChildren())
            {
                if (x >= 8)
                {
                    x = 0;
                    y++;
                }
                Markers[y, x] = child;
                x++;
            }
        }

        #endregion

        for (int i = 0; i < 8; i++)
        for (int j = 0; j < 8; j++)
        {
            var entity = PiecesOnBoard[i, j];
            if (entity != null)
            {
                Entity.Scene.Entities.Remove(entity);
            }
        }

        var modelCache = new Dictionary<string, Model>();

        for (int i = 0; i < 8; i++)
        {
            CreatePiece("Pawn02", 1, i, $"WhitePawn_{i}", modelCache);
            PiecesOnBoard[2, i] = null;
            PiecesOnBoard[3, i] = null;
            PiecesOnBoard[4, i] = null;
            PiecesOnBoard[5, i] = null;
            CreatePiece("Pawn01", 6, i, $"BlackPawn_{i}", modelCache);
        }

        CreatePiece("Rook02", 0, 0, "WhiteRook_0", modelCache);
        CreatePiece("Rook02", 0, 7, "WhiteRook_1", modelCache);
        CreatePiece("Rook01", 7, 0, "BlackRook_0", modelCache);
        CreatePiece("Rook01", 7, 7, "BlackRook_1", modelCache);

        CreatePiece("Knight02", 0, 1, "WhiteKnight_0", modelCache);
        CreatePiece("Knight02", 0, 6, "WhiteKnight_1", modelCache);
        CreatePiece("Knight01", 7, 1, "BlackKnight_0", modelCache);
        CreatePiece("Knight01", 7, 6, "BlackKnight_1", modelCache);

        CreatePiece("Bishop02", 0, 2, "WhiteBishop_0", modelCache);
        CreatePiece("Bishop02", 0, 5, "WhiteBishop_1", modelCache);
        CreatePiece("Bishop01", 7, 2, "BlackBishop_0", modelCache);
        CreatePiece("Bishop01", 7, 5, "BlackBishop_1", modelCache);

        CreatePiece("Queen02", 0, 3, "WhiteQueen", modelCache);
        CreatePiece("Queen01", 7, 3, "BlackQueen", modelCache);

        CreatePiece("King02", 0, 4, "WhiteKing", modelCache);
        CreatePiece("King01", 7, 4, "BlackKing", modelCache);

        MovesLogic.InitBoard();
    }

    private void TrySelectPiece()
    {
        if (!Input.IsMouseButtonPressed(MouseButton.Left)) return;

        var (success, hitInfo) = TryGetMouseClickedEntity(CollisionMask.Layer1);
        var entity = success ? hitInfo.Collidable.Entity : null;
        if (entity != null && entity != Entity) // Board entity
        {
            var marker = GetMarkerClosestTo(entity.Transform.Position);
            if (MovesLogic.CanMove(marker))
            {
                SelectedMarker = marker;
                SelectedPiece = entity;
                entity.Get<BodyComponent>().CollisionLayer = CollisionLayer.Layer2;
            }
        }
    }

    private void DragPiece()
    {
        if (!Input.IsMouseButtonDown(MouseButton.Left) || SelectedPiece == null) return;

        var (success, hitInfo) = TryGetMouseClickedEntity(CollisionMask.Layer0 | CollisionMask.Layer1);
        if (success && hitInfo.Collidable.Entity == Entity) // Board entity
        {
            // this breaks the physics simulation but it's ok for just dragging
            SelectedPiece.Transform.Position = hitInfo.Point;
        }
    }

    private void DropPiece()
    {
        if (!Input.IsMouseButtonReleased(MouseButton.Left) || SelectedPiece == null) return;

        var (validMove, updates) = TryMovePiece();
        if (validMove)
        {
            foreach (var u in updates!)
            {
                switch (u.Type)
                {
                    case BoardUpdateType.Move:
                        var entity = PiecesOnBoard[u.From.Y, u.From.X] ?? throw new InvalidOperationException($"Tried to move non existing piece: Y={u.From.Y} X={u.From.X}");
                        var body = entity.Get<BodyComponent>();
                        var newPosition = Markers[u.To.Y, u.To.X].Transform.Position;
                        body.Teleport(newPosition, entity.Transform.Rotation);

                        PiecesOnBoard[u.To.Y, u.To.X] = entity;
                        break;

                    case BoardUpdateType.Remove:
                        var targetPieceRemove = PiecesOnBoard[u.From.Y, u.From.X];
                        PiecesOnBoard[u.From.Y, u.From.X] = null;
                        Entity.Scene.Entities.Remove(targetPieceRemove);
                        break;
                }
            }
        }
        else
        {
            // revert to original position
            SelectedPiece.Transform.Position = Markers[SelectedMarker.Y, SelectedMarker.X].Transform.Position;
        }

        SelectedPiece.Get<BodyComponent>().CollisionLayer = CollisionLayer.Layer1;
        SelectedPiece = null;
    }

    private (bool, List<BoardUpdate>?) TryMovePiece()
    {
        var (success, hitInfo) = TryGetMouseClickedEntity(CollisionMask.Layer0);
        var validPosition = success && hitInfo.Collidable.Entity == Entity; // Board entity
        if (!validPosition) return (false, null);

        var marker = GetMarkerClosestTo(hitInfo.Point);
        return MovesLogic.TryMovePiece(SelectedMarker, marker);
    }

    private PiecePosition GetMarkerClosestTo(Vector3 position)
    {
        float bestDistSq = float.MaxValue;
        int bestX = -1;
        int bestY = -1;

        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                var markerPos = Markers[y, x].Transform.Position;
                var distSq = Vector3.DistanceSquared(position, markerPos);

                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    bestX = x;
                    bestY = y;
                }
            }
        }

        const float maxAllowedDistSq = 0.25f;

        if (bestDistSq > maxAllowedDistSq)
            return new(-1, -1);

        return new(bestX, bestY);
    }

    private (bool success, HitInfo hitInfo) TryGetMouseClickedEntity(CollisionMask collisionMask = CollisionMask.Everything)
    {
        var ray = GetPickRay(Camera, Input.MousePosition);
        var simulation = Camera.Entity.GetSimulation();
        var success = simulation.RayCast(ray.Position, ray.Direction, 50f, out var hitInfo, collisionMask);
        return (success, hitInfo);

        static Ray GetPickRay(CameraComponent camera, Vector2 screenPosition)
        {
            var (nearPoint, farPoint) = ScreenPointToRay(camera, screenPosition);

            var direction = farPoint - nearPoint;

            direction.Normalize();

            return new Ray(nearPoint, direction);
        }

        static (Vector3 VectorNear, Vector3 VectorFar) ScreenPointToRay(CameraComponent camera, Vector2 screenPosition)
        {
            // Invert the view-projection matrix to transform from screen space to world space
            var invertedMatrix = Matrix.Invert(camera.ViewProjectionMatrix);

            // Reconstruct the projection-space position in the (-1, +1) range.
            // The X coordinate maps directly from screen space (0,1) to projection space (-1,+1).
            // The Y coordinate is inverted because screen space is Y-down, while projection space is Y-up.
            Vector3 position;
            position.X = screenPosition.X * 2f - 1f;
            position.Y = 1f - screenPosition.Y * 2f;

            // Set Z = 0 for the near plane (the starting point of the ray)
            // Unproject the near point from projection space to world space
            position.Z = 0f;
            var vectorNear = Vector3.Transform(position, invertedMatrix);
            vectorNear /= vectorNear.W;

            // Set Z = 1 for the far plane (the end point of the ray)
            // Unproject the far point from projection space to world space
            position.Z = 1f;
            var vectorFar = Vector3.Transform(position, invertedMatrix);
            vectorFar /= vectorFar.W;

            return (vectorNear.XYZ(), vectorFar.XYZ());
        }
    }
}
