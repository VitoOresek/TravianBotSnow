using HtmlAgilityPack;
using MainCore.Enums;
using MainCore.Parsers;

namespace MainCore.Test.Parsers
{
    public class BuildingNamesTest
    {
        [Theory]
        [InlineData("Lehmgrube", "ClayPit")]
        [InlineData("Eisenmine", "IronMine")]
        [InlineData("Holzfäller", "Woodcutter")]
        [InlineData("GroßeKaserne", "GreatBarracks")]
        [InlineData("Hauptgebäude", "MainBuilding")]
        [InlineData("Palisade", "Palisade")]
        [InlineData("ClayPit", "ClayPit")]
        [InlineData("SomethingNew", "SomethingNew")]
        public void ToEnumName(string name, string expected)
        {
            BuildingNames.ToEnumName(name).ShouldBe(expected);
        }

        [Fact]
        public void GetQueueBuilding_ShouldReadGermanNames()
        {
            // the saved English page with the queue names replaced by German, one written with an html entity
            var page = File.ReadAllText("Parsers/BuildingLayout/BuildingsWithQueue.html")
                .Replace("\t\t\t\t\tWarehouse\t\t\t\t\t<span class=\"lvl\">Level 1</span>", "\t\t\t\t\tHolzf&auml;ller\t\t\t\t\t<span class=\"lvl\">Stufe 1</span>")
                .Replace("\t\t\t\t\tGranary\t\t\t\t\t<span class=\"lvl\">Level 1</span>", "\t\t\t\t\tGroßer Kornspeicher\t\t\t\t\t<span class=\"lvl\">Stufe 1</span>");
            var html = new HtmlDocument();
            html.LoadHtml(page);

            var queue = MainCore.Parsers.BuildingLayoutParser.GetQueueBuilding(html).ToList();

            queue.Select(x => Enum.Parse<BuildingEnums>(x.Type)).ShouldBe([BuildingEnums.Woodcutter, BuildingEnums.GreatGranary]);
            queue.Select(x => x.Level).ShouldBe([1, 1]);
        }
    }
}
