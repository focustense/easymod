using System.Numerics;

namespace Focus.Graphics
{
    public struct Light
    {
        public float Intensity { get; set; } = 1.0f;
        public Vector3 Position { get; set; }

        public Light(Vector3 position, float intensity = 1.0f)
        {
            Position = position;
            Intensity = intensity;
        }
    }
}
