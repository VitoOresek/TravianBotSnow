using HtmlAgilityPack;

namespace MainCore.Test.Parsers
{
    public class UpgradeParserVideoUnavailableTest
    {
        [Fact]
        public void NoVideoAvailable_ShouldStillFindNormalUpgradeButton()
        {
            // same page as when Travian has no ad to show: the video button turns grey
            var page = File.ReadAllText("Parsers/Upgrade/CroplandConstructed.html")
                .Replace("textButtonV1 green build videoFeatureButton", "textButtonV1 grey build videoFeatureButton disabled");
            var html = new HtmlDocument();
            html.LoadHtml(page);

            MainCore.Parsers.UpgradeParser.GetSpecialUpgradeButton(html).ShouldBeNull();
            var normal = MainCore.Parsers.UpgradeParser.GetUpgradeButton(html);
            normal.ShouldNotBeNull();
            normal.HasClass("videoFeatureButton").ShouldBeFalse();
        }
    }
}
