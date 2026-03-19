namespace RiotPicker.Models;

public static class RoleData
{
    public static readonly string[] Roles = ["TOP", "JUNGLE", "MIDDLE", "BOTTOM", "UTILITY"];

    public static readonly Dictionary<string, string> RoleDisplay = new()
    {
        ["TOP"] = "TOP",
        ["JUNGLE"] = "JGL",
        ["MIDDLE"] = "MID",
        ["BOTTOM"] = "ADC",
        ["UTILITY"] = "SUP",
    };

    public static readonly Dictionary<string, string> RoleFromDisplay = new()
    {
        ["TOP"] = "TOP",
        ["JGL"] = "JUNGLE",
        ["MID"] = "MIDDLE",
        ["ADC"] = "BOTTOM",
        ["SUP"] = "UTILITY",
    };
}
