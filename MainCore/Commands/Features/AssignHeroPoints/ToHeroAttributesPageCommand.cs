#pragma warning disable S1172

namespace MainCore.Commands.Features.AssignHeroPoints
{
    [Handler]
    public static partial class ToHeroAttributesPageCommand
    {
        public sealed record Command : ICommand;

        private static async ValueTask<Result> HandleAsync(
            Command command,
            IChromeBrowser browser,
            IDelayService delayService,
            CancellationToken cancellationToken)
        {
            if (HeroParser.IsAttributesPage(browser.Html)) return Result.Ok();

            var (_, isFailed, element, errors) = await browser.GetElement(doc => InventoryParser.GetHeroAvatar(doc), cancellationToken);
            if (isFailed) return Result.Fail(errors);

            var result = await browser.Click(element, cancellationToken);
            if (result.IsFailed) return result;

            static bool HeroPageShown(IWebDriver driver)
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(driver.PageSource);
                return HeroParser.IsAttributesPage(doc) || doc.GetElementbyId("heroV2") is not null;
            }

            result = await browser.Wait(HeroPageShown, cancellationToken);
            if (result.IsFailed) return result;

            if (!HeroParser.IsAttributesPage(browser.Html))
            {
                await delayService.DelayClick(cancellationToken);

                if (HeroParser.GetAttributesTab(browser.Html) is not null)
                {
                    (_, isFailed, element, errors) = await browser.GetElement(doc => HeroParser.GetAttributesTab(doc), cancellationToken);
                    if (isFailed) return Result.Fail(errors);

                    result = await browser.Click(element, cancellationToken);
                    if (result.IsFailed) return result;
                }
                else
                {
                    var server = new Uri(browser.CurrentUrl).GetLeftPart(UriPartial.Authority);
                    result = await browser.Navigate($"{server}/hero/attributes", cancellationToken);
                    if (result.IsFailed) return result;
                }
            }

            static bool AttributesShown(IWebDriver driver)
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(driver.PageSource);
                return HeroParser.IsAttributesPage(doc);
            }

            result = await browser.Wait(AttributesShown, cancellationToken);
            if (result.IsFailed) return result;

            await delayService.DelayTask(cancellationToken);
            return Result.Ok();
        }
    }
}
