using System;
using System.Collections.Generic;
using System.Linq;

using Rnd = UnityEngine.Random;

namespace Variety
{
    public class BulbFactory : ItemFactory
    {
        public override Item Generate(VarietyModule module, HashSet<object> taken, Random rnd)
        {
            var availableColors = ((Bulb.BulbColor[])Enum.GetValues(typeof(Bulb.BulbColor))).Where(col => !taken.Contains(col)).ToArray();
            if (availableColors.Length == 0)
                return null;

            var availableSpots = Enumerable.Range(0, W * H).Where(spot => isRectAvailable(taken, spot, 2, 2)).ToArray();
            if (availableSpots.Length == 0)
                return null;

            var topLeftCell = availableSpots[rnd.Next(0, availableSpots.Length)];
            var color = availableColors[rnd.Next(0, availableColors.Length)];
            claimRect(taken, topLeftCell, 2, 2);
            taken.Add(color);
            var x = (float)rnd.NextDouble() * 1.5f;
            int y = UnityEngine.Mathf.CeilToInt(UnityEngine.Mathf.Pow(2, 5f - x) * 26f / 32f);
            return new Bulb(module, topLeftCell, color, y);
        }

        public override IEnumerable<object> Flavors { get { return Enum.GetValues(typeof(Bulb.BulbColor)).Cast<object>(); } }
    }
}
