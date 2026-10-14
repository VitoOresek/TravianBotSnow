using HtmlAgilityPack;
using MainCore.Parsers;

namespace MainCore.Test.Parsers
{
    public class HeroParserTest
    {
        private static HtmlDocument Load(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            return doc;
        }

        [Theory]
        [InlineData("<div id='heroImageButton'><i class='levelUp show'></i></div>", true)]
        [InlineData("<div class='bigSpeechBubble levelUp'>Level up!</div>", true)]
        [InlineData("<span class='levelUp show'></span>", true)]
        [InlineData("<div id='heroImageButton'><i class='levelUp'></i></div>", false)]
        [InlineData("<div class='bigSpeechBubble adventure'></div>", false)]
        [InlineData("<div id='heroImageButton'></div>", false)]
        public void HasLevelUp(string html, bool expected)
        {
            HeroParser.HasLevelUp(Load(html)).ShouldBe(expected);
        }

        [Fact]
        public void IsAttributesPage()
        {
            HeroParser.IsAttributesPage(Load("<div class='heroAttributes'><input name='power' value='0'></div>")).ShouldBeTrue();
            HeroParser.IsAttributesPage(Load("<div id='heroV2'><div class='inventoryPageWrapper'></div></div>")).ShouldBeFalse();
        }

        [Fact]
        public void GetAttributesTab()
        {
            var doc = Load("<div id='heroV2'><a class='tabItem' href='/hero/inventory'>Inventory</a><a class='tabItem' href='/hero/attributes'>Attributes</a></div>");
            HeroParser.GetAttributesTab(doc)!.InnerText.ShouldBe("Attributes");
            HeroParser.GetAttributesTab(Load("<a href='/hero/inventory'>Inventory</a>")).ShouldBeNull();
        }
    }
}
