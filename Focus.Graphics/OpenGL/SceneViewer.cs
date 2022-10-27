using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System.Numerics;

namespace Focus.Graphics.OpenGL
{
    public enum CameraMode
    {
        Orthographic,
        Perspective,
    }

    public class SceneViewer
    {
        public float CameraDistance { get; set; } = 100f;
        // Zoom is actually inverted, but this is easier to work with.
        public float CameraZoom { get; set; } = 1.2f;
        public CameraMode CameraMode { get; set; } = CameraMode.Orthographic;
        public Vector2 CameraOffset { get; set; }
        public Vector3 ModelRotationAngles { get; set; }
        public Vector2 ViewportSize { get; set; } = new(1280, 720);

        private readonly IRenderer renderer;

        public SceneViewer(IRenderer renderer)
        {
            this.renderer = renderer;
        }

        public IDisposable AttachToView(IView view)
        {
            return new ViewEventHandlers(this, view);
        }

        private Vector2 ComputeIdealViewportSize()
        {
            var modelSize = renderer.GetModelSize();
            var width = modelSize.X;
            var height = modelSize.Y;
            var aspectRatio = ViewportSize.X / ViewportSize.Y;
            return Vector2.Max(
                new Vector2(width, width / aspectRatio),
                new Vector2(height * aspectRatio, height))
                * CameraZoom;
        }

        private Matrix4x4 ComputeModelRotation()
        {
            return Matrix4x4.CreateRotationX(ModelRotationAngles.X)
                * Matrix4x4.CreateRotationY(ModelRotationAngles.Y)
                * Matrix4x4.CreateRotationZ(ModelRotationAngles.Z);
        }

        private Matrix4x4 ComputeModelTransform()
        {
            var center = renderer.GetModelCenter();
            var offsetZ = ComputeModelZOffset();
            var translation = Matrix4x4.CreateTranslation(
                new Vector3(-center.X, -center.Y, -center.Z + offsetZ));
            var rotation = ComputeModelRotation();
            return translation * rotation;
        }

        private float ComputeModelZOffset()
        {
            // We want to center the model so that it's X/Y rotation axis is at the origin, but have
            // the bottom of the bounding box at the origin (rather than the Z center).
            // This way it doesn't intersect with the grid when we render it.
            return renderer.GetModelBounds().GetSize().Z / 2.0f;
        }

        private Matrix4x4 ComputeViewTransform()
        {
            // Because the model transform pushes the Z value upward - from having the model's
            // center at origin to having the bottom of its bounding box at origin - we want to
            // compensate for that by having the camera raised, and looking at a raised point.
            // Note, however, that the model may have been rotated, and "Z" in model space may be a
            // different direction in world space. What we actually need to do is take the model
            // rotation (not translation) and apply that to a Z transform in order to get the proper
            // direction.
            var modelOffsetZ = ComputeModelZOffset();
            var extraOffset =
                (Matrix4x4.CreateTranslation(0, 0, modelOffsetZ) * ComputeModelRotation())
                .Translation;
            var cameraPosition =
                new Vector3(CameraOffset.X, CameraOffset.Y, -CameraDistance) + extraOffset;
            var cameraTarget = new Vector3(CameraOffset.X, CameraOffset.Y, 0) + extraOffset;
            return Matrix4x4.CreateLookAt(cameraPosition, cameraTarget, Vector3.UnitY);
        }

        private Matrix4x4 GetProjection()
        {
            var cameraSize = ComputeIdealViewportSize();
            switch (CameraMode)
            {
                case CameraMode.Orthographic:
                    return Matrix4x4.CreateOrthographic(cameraSize.X, cameraSize.Y, 0.1f, 1000f);
                case CameraMode.Perspective:
                    return Matrix4x4.CreatePerspective(cameraSize.X, cameraSize.Y, 0.1f, 1000f);
                default:
                    throw new Exception($"Unsupported camera mode: {CameraMode}");
            }
        }

        private void Render()
        {
            var model = ComputeModelTransform();
            var view = ComputeViewTransform();
            var projection = GetProjection();
            renderer.Render(model, view, projection);
        }

        private class ViewEventHandlers : IDisposable
        {
            private const float mousePanningSensitivity = 0.1f;
            private const float mouseRotationSensitivity = 0.01f;
            private const float mouseWheelSensitivity = 0.1f;
            private const float mouseZoomingSensitivity = 0.01f;

            private readonly IView view;
            private readonly SceneViewer viewer;
            private readonly GL gl;

            private readonly HashSet<MouseButton> mouseButtonsPressed = new();

            private bool isDragging = false;
            private Vector2 previousMousePosition;

            public ViewEventHandlers(SceneViewer viewer, IView view)
            {
                this.view = view;
                this.viewer = viewer;
                gl = GL.GetApi(view);

                view.FramebufferResize += OnFramebufferResize;
                OnFramebufferResize(view.FramebufferSize);
                view.Render += OnRender;
                var input = view.CreateInput();
                foreach (var mouse in input.Mice)
                {
                    mouse.MouseDown += OnMouseDown;
                    mouse.MouseUp += OnMouseUp;
                    mouse.MouseMove += OnMouseMove;
                    mouse.Scroll += OnMouseScroll;
                }
            }

            public void Dispose()
            {
                using var input = view.CreateInput();
                foreach (var mouse in input.Mice)
                {
                    mouse.MouseDown -= OnMouseDown;
                    mouse.MouseUp -= OnMouseUp;
                    mouse.MouseMove -= OnMouseMove;
                    mouse.Scroll -= OnMouseScroll;
                }
                view.Render -= OnRender;
                view.FramebufferResize -= OnFramebufferResize;
                GC.SuppressFinalize(this);
            }

            private void OnFramebufferResize(Vector2D<int> size)
            {
                viewer.ViewportSize = new(size.X, size.Y);
            }

            private void OnMouseDown(IMouse mouse, MouseButton button)
            {
                mouseButtonsPressed.Add(button);
                isDragging = true;
                previousMousePosition = mouse.Position;
            }

            private void OnMouseMove(IMouse mouse, Vector2 position)
            {
                if (!isDragging)
                    return;
                var deltaPosition = position - previousMousePosition;
                previousMousePosition = position;
                if (mouseButtonsPressed.Contains(MouseButton.Left))
                {
                    viewer.ModelRotationAngles += new Vector3(
                        -deltaPosition.Y * mouseRotationSensitivity,
                        deltaPosition.X * mouseRotationSensitivity,
                        0);
                }
                else if (mouseButtonsPressed.Contains(MouseButton.Middle))
                {
                    viewer.CameraOffset += new Vector2(
                        deltaPosition.X * mousePanningSensitivity,
                        deltaPosition.Y * mousePanningSensitivity);
                }
                else if (mouseButtonsPressed.Contains(MouseButton.Right))
                {
                    viewer.CameraZoom +=
                        deltaPosition.Y * mouseZoomingSensitivity
                        - deltaPosition.X * mouseZoomingSensitivity;
                }
            }

            private void OnMouseUp(IMouse mouse, MouseButton button)
            {
                mouseButtonsPressed.Remove(button);
                isDragging = mouseButtonsPressed.Count > 0;
            }

            private void OnMouseScroll(IMouse mouse, ScrollWheel wheel)
            {
                viewer.CameraZoom = Math.Clamp(
                    viewer.CameraZoom - wheel.Y * mouseWheelSensitivity, 0.1f, 100);
            }

            private void OnRender(double deltaTime)
            {
                gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                viewer.Render();
            }
        }
    }
}
