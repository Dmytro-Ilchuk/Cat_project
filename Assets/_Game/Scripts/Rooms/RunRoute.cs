using System;
using System.Collections.Generic;
using System.Linq;

namespace NineLives
{
    public struct RouteStep
    {
        public RoomDefinition Room;
        public int ChunkSeed;
        public string LayoutId;
    }

    public static class RunRoute
    {
        // Nine route positions; only position six depends on the player's branch choice.
        private static readonly string[] Rooms =
        {
            "street_entrance", "street_crosswalk", "canal_steps", "street_roofs",
            "canal_trial", null, "canal_steps", "street_merchant", "canal_boss"
        };

        public static RouteStep[] Build(int seed, int branch, RoomDefinition[] definitions, RoomChunk[] chunks)
        {
            if (branch != 0 && branch != 1) throw new ArgumentOutOfRangeException(nameof(branch));
            var byId = definitions.Where(d => d != null).ToDictionary(d => d.StableId, StringComparer.Ordinal);
            var result = new RouteStep[Rooms.Length];
            var used = new HashSet<string>(StringComparer.Ordinal);
            var random = new Random(seed);
            for (int i = 0; i < result.Length; i++)
            {
                string id = i == 5 ? (branch == 0 ? "street_crosswalk" : "canal_steps") : Rooms[i];
                var room = byId[id];
                int chunkSeed = random.Next();
                string layoutId = id;
                if (room.Kind == RoomKind.Combat)
                {
                    // At most three fights per biome with the current library of six ordered layouts.
                    for (int attempt = 0; attempt < 256; attempt++, chunkSeed++)
                    {
                        layoutId = ChunkRoomBuilder.Signature(room, chunkSeed, chunks);
                        if (used.Add(layoutId)) break;
                    }
                    if (!used.Contains(layoutId) || result.Take(i).Any(s => s.LayoutId == layoutId))
                        throw new InvalidOperationException("Not enough unique combat layouts for the route.");
                }
                result[i] = new RouteStep { Room = room, ChunkSeed = chunkSeed, LayoutId = layoutId };
            }
            return result;
        }
    }
}
