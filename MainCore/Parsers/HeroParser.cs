namespace MainCore.Parsers
{
    public static class HeroParser
    {
        /// <summary>Hero attribute inputs on /hero/attributes, in the order Travian shows them.</summary>
        public static readonly string[] AttributeFields = ["power", "offBonus", "defBonus", "productionPoints"];

        /// <summary>
        /// The level up badge Travian shows on the hero avatar / speech bubble while unspent attribute points exist.
        /// </summary>
        public static bool HasLevelUp(HtmlDocument doc)
        {
            return doc.DocumentNode
                .Descendants()
                .Any(x => x.HasClass("levelUp") && (x.HasClass("show") || x.HasClass("bigSpeechBubble")));
        }

        public static bool IsAttributesPage(HtmlDocument doc)
        {
            return GetAttributesPanel(doc) is not null;
        }

        public static HtmlNode? GetAttributesPanel(HtmlDocument doc)
        {
            return doc.DocumentNode
                .Descendants("div")
                .FirstOrDefault(x => x.HasClass("heroAttributes"));
        }

        public static HtmlNode? GetAttributesTab(HtmlDocument doc)
        {
            return doc.DocumentNode
                .Descendants("a")
                .FirstOrDefault(x => x.GetAttributeValue("href", "").Contains("/hero/attributes"));
        }
    }
}
