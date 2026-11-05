using System;
using System.Linq;

namespace NineLives
{
    public static class RunRoute
    {
        private static readonly string[] Rooms =
        {
            "street_entrance", "street_crosswalk", "street_roofs", "canal_steps", "canal_boss"
        };

        public static int RoomCount => Rooms.Length;

        public static RoomDefinition[] Build(RoomDefinition[] definitions)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            var byId = definitions.Where(room => room != null)
                .ToDictionary(room => room.StableId, StringComparer.Ordinal);
            return Rooms.Select(id =>
            {
                if (!byId.TryGetValue(id, out var room) || room.Prefab == null)
                    throw new InvalidOperationException("Campaign room or prefab is missing: " + id);
                return room;
            }).ToArray();
        }
    }
}
