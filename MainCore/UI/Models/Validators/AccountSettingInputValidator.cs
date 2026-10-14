using MainCore.UI.Models.Input;

namespace MainCore.UI.Models.Validators
{
    public class AccountSettingInputValidator : AbstractValidator<AccountSettingInput>
    {
        public AccountSettingInputValidator()
        {
            RuleFor(x => x.ClickDelay.Min)
                .LessThanOrEqualTo(x => x.ClickDelay.Max)
                .WithMessage("Minimum click delay ({PropertyValue}) should be less than maximum click delay ({ComparisonValue})");
            RuleFor(x => x.ClickDelay.Min)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Minimum click delay ({PropertyValue}) should be positive number");
            RuleFor(x => x.TaskDelay.Min)
                .LessThanOrEqualTo(x => x.TaskDelay.Max)
                .WithMessage("Minimum task delay ({PropertyValue}) should be less than maximum task delay ({ComparisonValue})");
            RuleFor(x => x.TaskDelay.Min)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Minimum task delay ({PropertyValue}) should be positive number");
            RuleFor(x => x.Tribe.SelectedItem.Tribe)
                .NotEqual(TribeEnums.Any)
                .WithMessage("Tribe should be specific");

            RuleFor(x => x.FarmInterval.Min)
                .LessThanOrEqualTo(x => x.FarmInterval.Max)
                .WithMessage("Minimum farm interval ({PropertyValue}) should be less than maximum farm interval ({ComparisonValue})");
            RuleFor(x => x.FarmInterval.Min)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Minimum farm interval ({PropertyValue}) should be positive number");

            RuleFor(x => x.HeroPointsFightingStrength.Value)
                .InclusiveBetween(0, HeroPointAllocation.PointsPerLevel)
                .WithMessage("Fighting strength hero points ({PropertyValue}) should be between 0 and 4");
            RuleFor(x => x.HeroPointsOffBonus.Value)
                .InclusiveBetween(0, HeroPointAllocation.PointsPerLevel)
                .WithMessage("Offence bonus hero points ({PropertyValue}) should be between 0 and 4");
            RuleFor(x => x.HeroPointsDefBonus.Value)
                .InclusiveBetween(0, HeroPointAllocation.PointsPerLevel)
                .WithMessage("Defence bonus hero points ({PropertyValue}) should be between 0 and 4");
            RuleFor(x => x.HeroPointsResources.Value)
                .InclusiveBetween(0, HeroPointAllocation.PointsPerLevel)
                .WithMessage("Resources hero points ({PropertyValue}) should be between 0 and 4");
            RuleFor(x => x.HeroPointsFightingStrength.Value + x.HeroPointsOffBonus.Value + x.HeroPointsDefBonus.Value + x.HeroPointsResources.Value)
                .Equal(HeroPointAllocation.PointsPerLevel)
                .OverridePropertyName("HeroPoints")
                .WithMessage("Hero points per level should add up to 4 (now {PropertyValue})");
        }
    }
}