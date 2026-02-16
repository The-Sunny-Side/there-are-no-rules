using System.Collections.Generic;

public static class AnchorNames
{
    public const string WeaponLeftAnchor = "armorLeftAnchor";
    public const string WeaponRightAnchor = "armorRightAnchor";
    public const string WeaponFrontAnchor = "armorFrontAnchor";
    public const string WeaponBackAnchor = "armorBackAnchor";
    public const string BaseAnchor = "baseAnchor";
    public const string BodyAnchor = "bodyAnchor";
    public const string StandardAnchor = "anchor";

    public static readonly Dictionary<string, string> DisplayNames = new Dictionary<string, string>
    {
        { "Standard", StandardAnchor },
        { "Base", BaseAnchor },
        { "Corpo", BodyAnchor },
        { "Arma Sinistra", WeaponLeftAnchor },
        { "Arma Destra", WeaponRightAnchor },
        { "Arma Frontale", WeaponFrontAnchor },
        { "Arma Posteriore", WeaponBackAnchor },
    };
}