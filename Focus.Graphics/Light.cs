using System.Numerics;

namespace Focus.Graphics
{
    public struct Light
    {
        public Vector3 Position { get; set; }

        public Light(Vector3 position)
        {
            Position = position;
        }
    }
}
