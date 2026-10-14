namespace MainCore.Commands.Features.AssignHeroPoints
{
    /// <summary>
    /// Spends unspent hero attribute points on /hero/attributes, ported from SnowWhite-Travian-Tools.
    /// Every + click is verified against the live attribute values and the free point counter before Save is pressed;
    /// anything unexpected skips the task without saving, so no point ever lands on the wrong attribute.
    /// </summary>
    [Handler]
    public static partial class AssignHeroPointsCommand
    {
        public sealed record Command(AccountId AccountId) : IAccountCommand;

        private static async ValueTask<Result> HandleAsync(
            Command command,
            IChromeBrowser browser,
            ISettingService settingService,
            IDelayService delayService,
            CancellationToken cancellationToken)
        {
            var accountId = command.AccountId;
            var (_, isFailed, allocation, errors) = HeroPointAllocation.Create(
                settingService.ByName(accountId, AccountSettingEnums.HeroPointsFightingStrength),
                settingService.ByName(accountId, AccountSettingEnums.HeroPointsOffBonus),
                settingService.ByName(accountId, AccountSettingEnums.HeroPointsDefBonus),
                settingService.ByName(accountId, AccountSettingEnums.HeroPointsResources));
            if (isFailed) return Skip.Error.WithErrors(errors);

            return await AssignAsync(browser, allocation, delayService, cancellationToken);
        }

        public static async Task<Result> AssignAsync(
            IChromeBrowser browser,
            HeroPointAllocation allocation,
            IDelayService delayService,
            CancellationToken cancellationToken)
        {
            var driver = browser.Driver;
            if (driver is null) return Stop.DriverNotReady;

            var state = await WaitForState(driver, _ => true, cancellationToken);
            if (state is null) return Skip.Error.WithError("Cannot read the hero attributes and free points, nothing assigned");

            var (free, levels) = state.Value;
            if (free == 0)
            {
                browser.Logger.Information("No unspent hero points");
                return Result.Ok();
            }

            var initial = free;
            var clicks = 0;
            while (free > 0)
            {
                var field = allocation.Next(clicks, levels);
                if (field is null) break;

                if (FindPlusButton(driver, field) is not IWebElement plus)
                {
                    return Skip.Error.WithError($"No unique enabled + button for {field}, stopped without saving");
                }

                var result = await browser.Click(plus, cancellationToken);
                if (result.IsFailed) return result;

                var expectedFree = free - 1;
                var before = levels;
                var latest = await WaitForState(driver, x => x.Free == expectedFree, cancellationToken);
                if (latest is null
                    || latest.Value.Levels[field] != before[field] + 1
                    || HeroParser.AttributeFields.Any(x => x != field && latest.Value.Levels[x] != before[x]))
                {
                    return Skip.Error.WithError("Hero attributes changed unexpectedly, Save was not clicked. Check the hero page before retrying");
                }

                (free, levels) = latest.Value;
                clicks++;
                await delayService.DelayClick(cancellationToken);
            }

            if (clicks == 0)
            {
                browser.Logger.Information("Hero attributes chosen for the points are already at their maximum");
                return Result.Ok();
            }

            if (driver.FindElements(By.Id("savePoints")) is not [var save] || !save.Enabled)
            {
                return Skip.Error.WithError("Hero Save button unavailable, Save was not clicked");
            }

            var saveResult = await browser.Click(save, cancellationToken);
            if (saveResult.IsFailed) return saveResult;

            await delayService.DelayTask(cancellationToken);

            saveResult = await browser.Refresh(cancellationToken);
            if (saveResult.IsFailed) return saveResult;

            var saved = await WaitForState(driver, _ => true, cancellationToken);
            if (saved is null
                || saved.Value.Free != initial - clicks
                || HeroParser.AttributeFields.Any(x => saved.Value.Levels[x] != levels[x]))
            {
                return Skip.Error.WithError("Hero points could not be confirmed after saving, check the hero page");
            }

            browser.Logger.Information("Assigned {Clicks} hero points (fighting strength/offence/defence/resources: {Power}/{Off}/{Def}/{Resources}), {Remaining} left",
                clicks, levels["power"], levels["offBonus"], levels["defBonus"], levels["productionPoints"], saved.Value.Free);
            return Result.Ok();
        }

        private readonly record struct HeroState(int Free, IReadOnlyDictionary<string, int> Levels);

        // values are read from the live DOM: the inputs' value property is not mirrored into the page source
        private const string ReadStateScript = """
            const root = document.querySelector('.heroAttributes');
            if (!root) return null;
            const levels = {};
            for (const name of ['power', 'offBonus', 'defBonus', 'productionPoints']) {
              const inputs = root.querySelectorAll('input[name="' + name + '"]');
              if (inputs.length !== 1 || !/^\d+$/.test(inputs[0].value || '')) return null;
              levels[name] = Number(inputs[0].value);
            }
            const counter = root.querySelector('.pointsAvailable');
            if (!counter) {
              // newer layouts hide the counter only when nothing is left to spend
              const save = root.querySelector('#savePoints') || document.querySelector('#savePoints');
              if (save && save.disabled && !document.querySelector('.bigSpeechBubble.levelUp, i.levelUp.show, .levelUp.show'))
                return { free: 0, levels };
              return null;
            }
            const digits = (counter.textContent || '').match(/\d+/);
            if (!digits) return null;
            return { free: Number(digits[0]), levels };
            """;

        // the + button that belongs to exactly this attribute, never the nth + button of the page
        private const string FindPlusScript = """
            const field = arguments[0];
            const selector = 'input[name="power"],input[name="offBonus"],input[name="defBonus"],input[name="productionPoints"]';
            const plus = 'button.plus, button.textButtonV2.plus';
            const root = document.querySelector('.heroAttributes');
            if (!root) return null;
            const inputs = root.querySelectorAll('input[name="' + field + '"]');
            if (inputs.length !== 1) return null;
            const input = inputs[0];
            const usable = b => b && !b.disabled && b.getAttribute('aria-disabled') !== 'true' && !b.classList.contains('disabled');
            const single = items => items.length === 1 && usable(items[0]) ? items[0] : null;
            for (let node = input.parentElement, depth = 0; node && depth < 8; node = node.parentElement, depth++) {
              if (node.querySelectorAll(selector).length !== 1) continue;
              const buttons = [...node.querySelectorAll(plus)];
              if (buttons.length === 1) return single(buttons);
              if (buttons.length > 1) return null;
            }
            // some layouts put the + button in a sibling before the input: take the only one between the previous attribute input and this one
            const attributes = [...root.querySelectorAll(selector)];
            const index = attributes.indexOf(input);
            if (index < 0) return null;
            const previous = attributes[index - 1] || null;
            const buttons = [...root.querySelectorAll(plus)].filter(button =>
              (button.compareDocumentPosition(input) & Node.DOCUMENT_POSITION_FOLLOWING) &&
              (!previous || (previous.compareDocumentPosition(button) & Node.DOCUMENT_POSITION_FOLLOWING)));
            return single(buttons);
            """;

        private static IWebElement? FindPlusButton(IWebDriver driver, string field)
        {
            return ((IJavaScriptExecutor)driver).ExecuteScript(FindPlusScript, field) as IWebElement;
        }

        private static HeroState? ReadState(IWebDriver driver)
        {
            if (((IJavaScriptExecutor)driver).ExecuteScript(ReadStateScript) is not Dictionary<string, object> raw) return null;
            if (raw["levels"] is not Dictionary<string, object> rawLevels) return null;

            var free = Convert.ToInt32(raw["free"]);
            if (free < 0 || free > 400) return null;

            var levels = HeroParser.AttributeFields.ToDictionary(x => x, x => Convert.ToInt32(rawLevels[x]));
            return new HeroState(free, levels);
        }

        private static async Task<HeroState?> WaitForState(IWebDriver driver, Func<HeroState, bool> condition, CancellationToken cancellationToken)
        {
            var deadline = DateTime.Now.AddSeconds(10);
            HeroState? state;
            do
            {
                state = ReadState(driver);
                if (state is not null && condition(state.Value)) return state;
                await Task.Delay(250, cancellationToken);
            }
            while (DateTime.Now < deadline);
            return null;
        }
    }
}
