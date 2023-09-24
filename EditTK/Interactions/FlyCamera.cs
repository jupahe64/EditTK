using EditTK.Utils;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Input.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace EditTK.Interactions
{
    public class FlyCamera : ICamera
    {
        public Vector3? TurntableUpVector { get; set; } = Vector3.UnitY;

        public Vector3 Eye => _eye.current;
        public Vector3 Position => _eye.current;
        public Quaternion Rotation => _rot.current;

        public float Smoothness = 3;

        private (Vector3 current, Vector3 target) _eye;
        private (Quaternion current, Quaternion target) _rot = (Quaternion.Identity, Quaternion.Identity);

        public FlyCamera(Vector3 eye, Quaternion rotation)
        {
            _eye = (eye, eye);
            _rot = (rotation, rotation);
        }

        public FlyCamera(Vector3 eye, Vector3 lookat)
        {
            LookAt(eye, lookat);

            _eye.current = _eye.target;
            _rot.current = _rot.target;
        }

        public void LookAt(Vector3 eye, Vector3 lookat)
        {
            _eye.target = eye;
            SetLookDirection(Vector3.Normalize(eye - lookat));
        }

        public void SetLookDirection(Vector3 direction)
        {
            Debug.Assert(MathF.Abs(direction.LengthSquared() - 1) < 0.001f);

            Vector3 forward = Vector3.Transform(Vector3.UnitZ, _rot.target);
            _rot.target = MathUtils.GetRotationBetween(forward, direction) * _rot.target;
        }

        public void Update(float deltaSeconds, IInputContext input, bool sceneViewHovered)
        {
            (float yaw, float pitch) deltaRotation = default;
            (float right, float up, float forward) movement = default;

            if (ImGui.IsMouseDragging(ImGuiMouseButton.Right))
            {
                Vector2 delta = ImGui.GetIO().MouseDelta;
                deltaRotation.yaw = -delta.X * 0.002f;
                deltaRotation.pitch = -delta.Y * 0.002f;
            }

            var keyboard = input!.Keyboards[0];

            if (keyboard.IsKeyPressed(Key.W))
                movement.forward -= 1f;
            if (keyboard.IsKeyPressed(Key.S))
                movement.forward += 1f;

            if (keyboard.IsKeyPressed(Key.A))
                movement.right -= 1f;
            if (keyboard.IsKeyPressed(Key.D))
                movement.right += 1f;

            if (keyboard.IsKeyPressed(Key.Q))
                movement.up -= 1f;
            if (keyboard.IsKeyPressed(Key.E))
                movement.up += 1f;

            UpdateWithCustomInputs(deltaSeconds, deltaRotation, movement, sceneViewHovered);
        }

        public void UpdateWithCustomInputs(float deltaSeconds, 
            (float yaw, float pitch) deltaRotation, 
            (float right, float up, float forward) movement,
            bool sceneViewHovered)
        {
            var right = Vector3.Transform(Vector3.UnitX, _rot.target);
            var up = Vector3.Transform(Vector3.UnitY, _rot.target);
            var forward = Vector3.Transform(Vector3.UnitZ, _rot.target);

            var turntableUpVector = TurntableUpVector ?? up;

            if (Vector3.Dot(up, turntableUpVector) < 0)
                deltaRotation.yaw *= -1;

            if (sceneViewHovered)
            {
                _rot.target = Quaternion.CreateFromAxisAngle(right, deltaRotation.pitch) * _rot.target;
                _rot.target = Quaternion.CreateFromAxisAngle(turntableUpVector, deltaRotation.yaw) * _rot.target;

                float camMoveSpeed = (float)(0.4 * deltaSeconds * 60);
                _eye.target += movement.forward * Vector3.Transform(Vector3.UnitZ * camMoveSpeed, Rotation);
                _eye.target += movement.right * Vector3.Transform(Vector3.UnitX * camMoveSpeed, Rotation);
                _eye.target += movement.up * turntableUpVector * camMoveSpeed;
            }

            if (TurntableUpVector.HasValue)
            {
                //align camera orientation with turntable

                Vector3 turntableRightVector;
                if(MathF.Abs(Vector3.Dot(forward, turntableUpVector)) < 0.8f)
                {
                    turntableRightVector = Vector3.Normalize(Vector3.Cross(turntableUpVector, forward));
                }
                else
                {
                    turntableRightVector = Vector3.Normalize(MathUtils.ProjectOnPlane(
                        Vector3.Cross(up, turntableUpVector),
                        (turntableUpVector, Vector3.Zero)
                    ));
                }

                if (MathF.Abs(Vector3.Dot(right, turntableRightVector)) < 0.99999f)
                    _rot.target = MathUtils.GetRotationBetween(right, turntableRightVector) * _rot.target;
            }

            float smoothFactor = 1f / (1f + Smoothness);

            _rot.current = Quaternion.Slerp(_rot.current, _rot.target, 1 - (float)Math.Pow(1 - smoothFactor, deltaSeconds * 60));
            _eye.current = Vector3.Lerp(_eye.current, _eye.target, 1 - (float)Math.Pow(1 - smoothFactor, deltaSeconds * 60));
        }
    }
}
