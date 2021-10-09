using System;
using System.ComponentModel;
using System.Linq;

namespace Focus.Apps.EasyNpc
{
    public static class Description
    {
        public static string Of<T>(T value)
            where T : struct, Enum
        {
            var description = GetFrom(value);
            return !string.IsNullOrEmpty(description) ? description : Enum.GetName(value) ?? value.ToString();
        }

        private static string? GetFrom(object value)
        {
            if (value is null)
                return null;
            var field = value.GetType().GetField(value.ToString() ?? string.Empty);
            if (field is null)
                return null;
            var attributes = (DescriptionAttribute[])field.GetCustomAttributes(typeof(DescriptionAttribute), false);
            return attributes.Select(x => x.Description).NotNullOrEmpty().FirstOrDefault();
        }
    }
}
