using System;
using System.Collections.Generic;
using System.Linq;
using KModkit;

using Rnd = UnityEngine.Random;

namespace Variety
{
    public class DialFactory : ItemFactory
    {
        public override Item Generate(VarietyModule module, HashSet<object> taken, Random rnd)
        {
            var available = Enum.GetValues(typeof(DialColor)).Cast<DialColor>().Where(c => !taken.Contains(c)).ToArray();
            if (available.Length == 0)
                return null;

            var availableSpots = Enumerable.Range(0, W * H).Where(topleft => isRectAvailable(taken, topleft, 2, 2)).ToArray();
            if (availableSpots.Length == 0)
                return null;

            var spot = availableSpots[rnd.Next(0, availableSpots.Length)];
            claimRect(taken, spot, 2, 2);
            var color = available[rnd.Next(0, available.Length)];
            taken.Add(color);
            int n = rnd.Next(3, 7);

            return new Dial(module, spot, color, n, rnd);
        }

        public override IEnumerable<object> Flavors { get { return Enum.GetValues(typeof(DialColor)).Cast<object>(); } }
    }
}