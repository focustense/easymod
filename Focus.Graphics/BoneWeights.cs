using System.Collections;
using System.Collections.Immutable;

namespace Focus.Graphics
{
    public record Bone(string Name);

    public interface IBoneWeights : IEnumerable<KeyValuePair<Bone, float>>
    {
        float this[Bone bone] { get; }

        IEnumerable<Bone> Bones { get; }
    }

    public class BoneWeights : IBoneWeights
    {
        public static readonly IBoneWeights Empty = new EmptyBoneWeights();

        public static IBoneWeights FromWeights(IEnumerable<KeyValuePair<Bone, float>> weights)
        {
            var boneWeights = new BoneWeights();
            foreach (var pair in weights)
                boneWeights[pair.Key] = pair.Value;
            boneWeights.Normalize();
            return boneWeights;
        }

        private readonly Dictionary<Bone, float> weights = new();

        private bool isNormalized;

        public IEnumerable<Bone> Bones => weights.Keys;

        public float this[Bone bone]
        {
            get => weights.GetValueOrDefault(bone);
            private set
            {
                if (value > 0)
                    weights[bone] = value;
                else if (value == 0)
                    weights.Remove(bone);
                else
                    throw new ArgumentOutOfRangeException(
                        nameof(value), "Bone cannot have negative weight");
                isNormalized = false;
            }
        }

        private BoneWeights() { }

        public IEnumerator<KeyValuePair<Bone, float>> GetEnumerator()
        {
            return weights.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public BoneWeights Normalize()
        {
            // Floating-point imprecision could cause our weights not to add up to 1.0, even after
            // a previous attempt to normalize. We could also compare the difference to epsilon
            // values and decide on some fuzzy "normalized enough" threshold, but it's simpler and
            // probably more accurate to just make sure we don't normalize the same set of weights
            // more than once.
            if (isNormalized)
                return this;
            var totalWeight = weights.Values.Sum();
            if (totalWeight == 1 || totalWeight == 0)
                return this;
            var weightAdjustment = 1 / totalWeight;
            foreach (var boneName in weights.Keys)
                weights[boneName] *= weightAdjustment;
            isNormalized = true;
            return this;
        }
    }

    class EmptyBoneWeights : IBoneWeights
    {
        public float this[Bone bone] => 0;

        public IEnumerable<Bone> Bones => Enumerable.Empty<Bone>();

        public IEnumerator<KeyValuePair<Bone, float>> GetEnumerator()
        {
            yield break;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
