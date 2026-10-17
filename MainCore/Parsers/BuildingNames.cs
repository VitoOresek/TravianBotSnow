namespace MainCore.Parsers
{
    /// <summary>
    /// The build queue only shows building names in the game language. English names are the enum names already,
    /// this maps the German ones (Travian worlds like *.germany.travian.com cannot switch to English).
    /// Keys are written the way the queue parser reads them: letters and digits only.
    /// </summary>
    public static class BuildingNames
    {
        private static readonly Dictionary<string, BuildingEnums> German = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Holzfäller"] = BuildingEnums.Woodcutter,
            ["Lehmgrube"] = BuildingEnums.ClayPit,
            ["Eisenmine"] = BuildingEnums.IronMine,
            ["Getreidefarm"] = BuildingEnums.Cropland,
            ["Sägewerk"] = BuildingEnums.Sawmill,
            ["Ziegelei"] = BuildingEnums.Brickyard,
            ["Lehmbrennerei"] = BuildingEnums.Brickyard,
            ["Eisengießerei"] = BuildingEnums.IronFoundry,
            ["Getreidemühle"] = BuildingEnums.GrainMill,
            ["Bäckerei"] = BuildingEnums.Bakery,
            ["Rohstofflager"] = BuildingEnums.Warehouse,
            ["Kornspeicher"] = BuildingEnums.Granary,
            ["Waffenschmiede"] = BuildingEnums.Blacksmith,
            ["Schmiede"] = BuildingEnums.Smithy,
            ["Rüstungsschmiede"] = BuildingEnums.Smithy,
            ["Turnierplatz"] = BuildingEnums.TournamentSquare,
            ["Hauptgebäude"] = BuildingEnums.MainBuilding,
            ["Versammlungsplatz"] = BuildingEnums.RallyPoint,
            ["Marktplatz"] = BuildingEnums.Marketplace,
            ["Botschaft"] = BuildingEnums.Embassy,
            ["Kaserne"] = BuildingEnums.Barracks,
            ["Stall"] = BuildingEnums.Stable,
            ["Werkstatt"] = BuildingEnums.Workshop,
            ["Akademie"] = BuildingEnums.Academy,
            ["Versteck"] = BuildingEnums.Cranny,
            ["Rathaus"] = BuildingEnums.TownHall,
            ["Residenz"] = BuildingEnums.Residence,
            ["Palast"] = BuildingEnums.Palace,
            ["Schatzkammer"] = BuildingEnums.Treasury,
            ["Handelskontor"] = BuildingEnums.TradeOffice,
            ["GroßeKaserne"] = BuildingEnums.GreatBarracks,
            ["GroßerStall"] = BuildingEnums.GreatStable,
            ["Stadtmauer"] = BuildingEnums.CityWall,
            ["Erdwall"] = BuildingEnums.EarthWall,
            ["Steinmetz"] = BuildingEnums.StonemasonsLodge,
            ["Steinmetzhütte"] = BuildingEnums.StonemasonsLodge,
            ["Brauerei"] = BuildingEnums.Brewery,
            ["Fallensteller"] = BuildingEnums.Trapper,
            ["Heldenhof"] = BuildingEnums.HerosMansion,
            ["GroßesRohstofflager"] = BuildingEnums.GreatWarehouse,
            ["GroßerKornspeicher"] = BuildingEnums.GreatGranary,
            ["Weltwunder"] = BuildingEnums.WW,
            ["Pferdetränke"] = BuildingEnums.HorseDrinkingTrough,
            ["Steinwall"] = BuildingEnums.StoneWall,
            ["Behelfsmauer"] = BuildingEnums.MakeshiftWall,
            ["Behelfswall"] = BuildingEnums.MakeshiftWall,
            ["Kommandozentrale"] = BuildingEnums.CommandCenter,
            ["Wasserwerk"] = BuildingEnums.Waterworks,
            ["Krankenhaus"] = BuildingEnums.Hospital,
            ["Lazarett"] = BuildingEnums.Hospital,
        };

        /// <returns>the BuildingEnums name for a localized building name, or the input unchanged when it is unknown</returns>
        public static string ToEnumName(string name)
        {
            if (Enum.TryParse(name, false, out BuildingEnums _)) return name;
            return German.TryGetValue(name, out var type) ? type.ToString() : name;
        }
    }
}
