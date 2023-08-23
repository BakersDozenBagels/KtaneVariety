using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Rnd = UnityEngine.Random;

namespace Variety
{
    public class CoordinateGridFactory : ItemFactory
    {
        private const int MinWidth = 3;
        private const int MaxWidth = 4;
        private const int MinHeight = 3;
        private const int MaxHeight = 4;
        private const int NumWidths = MaxWidth - MinWidth + 1;
        private const int NumHeights = MaxHeight - MinHeight + 1;

        public override Item Generate(VarietyModule module, HashSet<object> taken, System.Random rnd)
        {
            var availableConfigs = (
                from width in Enumerable.Range(MinWidth, MaxWidth - MinWidth + 1)
                from height in Enumerable.Range(MinHeight, MaxHeight - MinHeight + 1)
                where !taken.Contains(string.Format("CoordinateGrid:{0}:{1}", width, height))
                from cell in Enumerable.Range(0, W * H)
                where isRectAvailable(taken, cell, width + 1, height + 1)
                select new { Cell = cell, Width = width, Height = height }).ToArray();

            if (availableConfigs.Length == 0)
                return null;

            var config = availableConfigs[rnd.Next(0, availableConfigs.Length)];

            claimRect(taken, config.Cell, config.Width + 1, config.Height + 1);
            taken.Add(string.Format("CoordinateGrid:{0}:{1}", config.Width, config.Height));

            var func = rnd.Next(0, 6);

            return new CoordinateGrid(module, config.Cell % W, config.Cell / W, config.Width, config.Height, func);
        }

        public override IEnumerable<object> Flavors
        {
            get
            {
                for (var w = MinWidth; w <= MaxWidth; w++)
                    for (var h = MinHeight; h <= MaxHeight; h++)
                        yield return string.Format("CoordinateGrid:{0}:{1}", w, h);
            }
        }
    }
}