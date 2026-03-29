using ZLinq;

namespace Avayomi.Core.Extensions;

public static class EnumExtensions
{
    extension(Enum @enum)
    {
        public IEnumerable<Enum> GetAllValues(bool orderByName = false) =>
            @enum.GetType().GetAllValues(orderByName);
    }

    extension(Type t)
    {
        public IEnumerable<Enum> GetAllValues(bool orderByName = false)
        {
            if (!t.IsEnum)
            {
                throw new ArgumentException($"{nameof(t)} must be an enum type");
            }

            var names = orderByName
                ? Enum.GetNames(t)
                    .AsValueEnumerable()
                    .OrderBy(e => e.ToString(), StringComparer.Ordinal)
                    .ToArray()
                : Enum.GetNames(t).AsValueEnumerable().ToArray();

            foreach (var name in names)
            {
                yield return (Enum)Enum.Parse(t, name);
            }
        }
    }
}
