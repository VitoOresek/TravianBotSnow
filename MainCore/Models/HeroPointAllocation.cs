namespace MainCore.Models
{
    /// <summary>
    /// Spreads the 4 points of every hero level over the attributes, e.g. 1 fighting strength + 3 resources
    /// gives the order power, productionPoints, productionPoints, productionPoints, repeated.
    /// </summary>
    public sealed class HeroPointAllocation
    {
        public const int PointsPerLevel = 4;
        public const int MaxAttributeLevel = 100;

        private readonly string[] _order;

        private HeroPointAllocation(string[] order)
        {
            _order = order;
        }

        public IReadOnlyList<string> Order => _order;

        public static Result<HeroPointAllocation> Create(int fightingStrength, int offBonus, int defBonus, int resources)
        {
            int[] weights = [fightingStrength, offBonus, defBonus, resources];
            if (weights.Any(x => x < 0 || x > PointsPerLevel) || weights.Sum() != PointsPerLevel)
            {
                return Result.Fail($"Hero points per level must add up to {PointsPerLevel} (now {fightingStrength}/{offBonus}/{defBonus}/{resources})");
            }

            var order = Parsers.HeroParser.AttributeFields
                .SelectMany((field, i) => Enumerable.Repeat(field, weights[i]))
                .ToArray();
            return new HeroPointAllocation(order);
        }

        /// <summary>
        /// The attribute for the next point, skipping attributes already at the maximum level.
        /// </summary>
        /// <returns>null when every planned attribute is maxed</returns>
        public string? Next(int index, IReadOnlyDictionary<string, int> levels)
        {
            for (var i = 0; i < _order.Length; i++)
            {
                var field = _order[(index + i) % _order.Length];
                if (levels[field] < MaxAttributeLevel) return field;
            }
            return null;
        }
    }
}
