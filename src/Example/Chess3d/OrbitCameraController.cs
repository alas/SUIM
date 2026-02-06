namespace Chess3d;

using System;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

public class OrbitCameraController : SyncScript
{
    private float distance = 2.730638f;
    private float pitch = 28f;
    private float yaw = 90f;

    public override void Update()
    {
        HandleYaw();
        HandlePitch();
        HandleZoom();
        UpdateTransform();
    }

    private void HandleYaw()
    {
        const float RotationalSpeed = 50f;
        float deltaTime = (float)Game.UpdateTime.Elapsed.TotalSeconds;
        if (Input.IsKeyDown(Keys.A) || Input.IsKeyDown(Keys.Left) || Input.IsKeyDown(Keys.NumPad4))
        {
            yaw -= (1 * deltaTime * RotationalSpeed);
        }
        if (Input.IsKeyDown(Keys.D) || Input.IsKeyDown(Keys.Right) || Input.IsKeyDown(Keys.NumPad6))
        {
            yaw += (1 * deltaTime * RotationalSpeed);
        }
        if (Input.HasGamePad)
        {
            GamePadState padState = Input.DefaultGamePad.State;
            var speedFactor = 1f;
            if ((padState.Buttons & (GamePadButton.A | GamePadButton.LeftShoulder | GamePadButton.RightShoulder)) != 0)
            {
                speedFactor *= 5.0f;
            }
            yaw += padState.LeftThumb.X * speedFactor;
        }

        if (Input.HasMouse)
        {
            if (Input.IsMouseButtonDown(MouseButton.Right))
            {
                Input.LockMousePosition();
                Game.IsMouseVisible = false;

                yaw -= Input.MouseDelta.X * 100f;
            }
            else
            {
                Input.UnlockMousePosition();
                Game.IsMouseVisible = true;
            }
        }

        foreach (var gestureEvent in Input.GestureEvents)
        {
            switch (gestureEvent.Type)
            {
                case GestureType.Drag:
                    var drag = (GestureEventDrag)gestureEvent;
                    yaw -= drag.DeltaTranslation.X;
                    break;

                case GestureType.Composite:
                    var composite = (GestureEventComposite)gestureEvent;
                    yaw = -composite.DeltaTranslation.X * 0.7f;
                    break;
            }
        }
    }

    private void HandlePitch()
    {
        //pitch = MathUtil.Clamp(pitch, 20f, 80f);
    }

    private void HandleZoom()
    {
        //distance -= Input.MouseWheelDelta * ZoomSpeed;
        //distance = MathUtil.Clamp(distance, 2f, 12f);
    }

    private void UpdateTransform()
    {
        var rotation = Quaternion.RotationYawPitchRoll(
            MathUtil.DegreesToRadians(yaw),
            MathUtil.DegreesToRadians(pitch),
            0);
        var direction = Vector3.UnitY * distance;
        Entity.Transform.Position = Vector3.Transform(direction, rotation);

        Entity.Transform.Rotation = GetAngleToTarget(Entity.Transform.Position, Vector3.Zero);
    }

    private static Quaternion GetAngleToTarget(Vector3 position, Vector3 target)
    {
        var (azimuth, altitude) = GetLookAtAngles(position, target);
        var result = Quaternion.RotationYawPitchRoll(azimuth, -altitude, 0);
        return result;
    }

    private static (float, float) GetLookAtAngles(Vector3 source, Vector3 destination)
    {
        var x = source.X - destination.X;
        var y = source.Y - destination.Y;
        var z = source.Z - destination.Z;

        var altitude = (float)Math.Atan2(y, Math.Sqrt(x * x + z * z));
        var azimuth = (float)Math.Atan2(x, z);
        return (azimuth, altitude);
    }
}
