namespace Focus.Tools.EasyFollower
{
    static class ListExtensions
    {
        public static T? GetOrDefault<T>(this IList<T> list, int index)
        {
            return list.Count > index ? list[index] : default;
        }
    }
}
