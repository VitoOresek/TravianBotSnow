using MainCore.Models;
using MainCore.UI.Models.Input;
using MainCore.UI.Models.Validators;

namespace MainCore.Test.Services
{
    public class HeroPointAllocationTest
    {
        private static Dictionary<string, int> Levels(int power = 0, int off = 0, int def = 0, int resources = 0) => new()
        {
            ["power"] = power,
            ["offBonus"] = off,
            ["defBonus"] = def,
            ["productionPoints"] = resources,
        };

        [Theory]
        [InlineData(0, 0, 0, 3)]
        [InlineData(0, 0, 0, 5)]
        [InlineData(2, 2, 2, -2)]
        [InlineData(5, 0, 0, -1)]
        public void Create_ShouldRejectSplitsThatAreNotFourPoints(int power, int off, int def, int resources)
        {
            HeroPointAllocation.Create(power, off, def, resources).IsFailed.ShouldBeTrue();
        }

        [Fact]
        public void Create_ShouldOrderPointsByAttribute()
        {
            var allocation = HeroPointAllocation.Create(1, 0, 1, 2).Value;
            allocation.Order.ShouldBe(["power", "defBonus", "productionPoints", "productionPoints"]);
        }

        [Fact]
        public void Next_ShouldCycleThroughTheOrder()
        {
            var allocation = HeroPointAllocation.Create(1, 0, 0, 3).Value;
            var levels = Levels();
            Enumerable.Range(0, 8).Select(i => allocation.Next(i, levels))
                .ShouldBe(["power", "productionPoints", "productionPoints", "productionPoints", "power", "productionPoints", "productionPoints", "productionPoints"]);
        }

        [Fact]
        public void Next_ShouldSkipMaxedAttributes()
        {
            var allocation = HeroPointAllocation.Create(2, 0, 0, 2).Value;
            allocation.Next(0, Levels(power: 100)).ShouldBe("productionPoints");
            allocation.Next(0, Levels(power: 100, resources: 100)).ShouldBeNull();
        }

        [Fact]
        public void Validator_ShouldRequireFourPointsPerLevel()
        {
            var validator = new AccountSettingInputValidator();
            var input = new AccountSettingInput();
            input.Tribe.Set(Enums.TribeEnums.Romans);
            input.HeroPointsResources.Set(4);
            var valid = validator.Validate(input);
            valid.IsValid.ShouldBeTrue(string.Join(" | ", valid.Errors.Select(x => x.ErrorMessage)));

            input.HeroPointsOffBonus.Set(1);
            var result = validator.Validate(input);
            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(x => x.ErrorMessage.Contains("add up to 4"));
        }
    }
}
