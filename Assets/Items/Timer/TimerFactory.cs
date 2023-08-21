﻿using System;
using System.Collections.Generic;
using System.Linq;

using Rnd = UnityEngine.Random;

namespace Variety
{
    class TimerFactory : ItemFactory
    {
        public override IEnumerable<object> Flavors
        {
            get
            {
                return Enum.GetValues(typeof(TimerType)).Cast<object>();
            }
        }

        public override Item Generate(VarietyModule module, HashSet<object> taken, Random rnd)
        {
            var availableCells = Enumerable.Range(0, W * H).Where(c => isRectAvailable(taken, c, 2, 1)).ToArray();
            if (availableCells.Length == 0)
                return null;

            var availableFlavors = ((TimerType[])Enum.GetValues(typeof(TimerType))).Where(c => !taken.Contains(c)).ToArray();
            if (availableFlavors.Length == 0)
                return null;

            var cell = availableCells[rnd.Next(0, availableCells.Length)];
            claimRect(taken, cell, 2, 1);

            var flavor = availableFlavors[rnd.Next(0, availableFlavors.Length)];
            taken.Add(flavor);

            int a;
            return new Timer(module, cell, flavor, a = rnd.Next(0, 4), rnd.Next(0, a == 3 ? 2 : a== 2 ? 3 : 4));
        }
    }
}