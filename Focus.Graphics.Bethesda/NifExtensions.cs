using nifly;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Numerics;

using NifVector2 = nifly.Vector2;
using NifVector3 = nifly.Vector3;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;

namespace Focus.Graphics.Bethesda
{
    internal static class NifExtensions
    {
        // Maps of flag values to our own enums (index equals the flag value)
        private static readonly AlphaBlendFactor[] alphaBlendFactors = new[]
        {
            AlphaBlendFactor.One,
            AlphaBlendFactor.Zero,
            AlphaBlendFactor.SourceColor,
            AlphaBlendFactor.OneMinusSourceColor,
            AlphaBlendFactor.DestinationColor,
            AlphaBlendFactor.OneMinusDestinationColor,
            AlphaBlendFactor.SourceAlpha,
            AlphaBlendFactor.OneMinusSourceAlpha,
            AlphaBlendFactor.DestinationAlpha,
            AlphaBlendFactor.OneMinusDestinationAlpha,
            AlphaBlendFactor.SourceAlphaSaturate,
        };

        private static readonly AlphaTestFunction[] alphaTestFunctions = new[]
        {
            AlphaTestFunction.Always,
            AlphaTestFunction.Less,
            AlphaTestFunction.Equal,
            AlphaTestFunction.LessOrEqual,
            AlphaTestFunction.Greater,
            AlphaTestFunction.NotEqual,
            AlphaTestFunction.GreaterOrEqual,
            AlphaTestFunction.Never,
        };

        public static Color ToColor(this NifVector3 v)
        {
            return Color.FromArgb(
                (int)Math.Round(v.x * 255), (int)Math.Round(v.y * 255), (int)Math.Round(v.z * 255));
        }

        public static AlphaBlendSettings GetAlphaBlendSettings(this NiAlphaProperty alphaProperty)
        {
            var flags = alphaProperty.flags;
            var blendingFlag = flags & 0x01;
            var sourceFlag = flags >> 1 & 0x0f;
            var destinationFlag = flags >> 5 & 0x0f;
            var testingFlag = flags & 0x200;
            var testModeFlag = flags >> 10 & 0x07;
            var testThreshold = alphaProperty.threshold / 255.0f;
            return new(
                blendingFlag != 0,
                GetAlphaBlendFactor(sourceFlag),
                GetAlphaBlendFactor(destinationFlag),
                testingFlag != 0,
                GetAlphaTestFunction(testModeFlag),
                testThreshold);
        }

        public static Matrix4x4 ToMat4(this MatTransform t)
        {
            // nifly doesn't appear to have any API for actually reading a matrix directly.
            // Our only choice is to convert the rotation to Euler, which is going to be somewhat lossy.
            float yaw = 0, pitch = 0, roll = 0;
            t.rotation.ToEulerAngles(ref yaw, ref pitch, ref roll);
            // Matrix4x4.CreateFromYawPitchRoll gives results that make no sense. This way seems inefficient
            // but it'll work until it can be determined WTF is going on with System.Numerics.
            var rotation =
                Matrix4x4.CreateRotationZ(roll)
                * Matrix4x4.CreateRotationY(pitch)
                * Matrix4x4.CreateRotationX(yaw);
            var translation =
                Matrix4x4.CreateTranslation(t.translation.x, t.translation.y, t.translation.z);
            return Matrix4x4.Transpose(rotation * translation * t.scale);
        }

        public static Vector2 ToVector2(this NifVector2 v, bool flipV = false)
        {
            return new Vector2(v.u, flipV ? v.v : 1 - v.v);
        }

        public static Vector3 ToVector3(this NifVector3 v)
        {
            return new Vector3(v.x, v.y, v.z);
        }

        public static bool TryGetBlock<T>(
            this NiHeader header, NiRef niRef, [MaybeNullWhen(false)] out T block)
            where T : NiObject
        {
            return header.TryGetBlock(niRef.index, out block);
        }

        public static bool TryGetBlock<T>(
            this NiHeader header, uint index, [MaybeNullWhen(false)] out T block)
            where T : NiObject
        {
            block = header.GetBlockById(index) as T;
            return block is not null;
        }

        private static AlphaBlendFactor GetAlphaBlendFactor(int flagValue)
        {
            return (flagValue >= 0) && (flagValue < alphaBlendFactors.Length)
                ? alphaBlendFactors[flagValue] :
                throw new ArgumentOutOfRangeException(nameof(flagValue));
        }

        private static AlphaTestFunction GetAlphaTestFunction(int flagValue)
        {
            return (flagValue >= 0) && (flagValue < alphaTestFunctions.Length)
                ? alphaTestFunctions[flagValue] :
                throw new ArgumentOutOfRangeException(nameof(flagValue));
        }
    }
}
