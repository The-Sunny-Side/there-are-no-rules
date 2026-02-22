using System.Collections.Generic;

public static class AnchorNames
{
    public const string WeaponLeftAnchor = "weaponLeftAnchor";
    public const string WeaponRightAnchor = "weaponRightAnchor";
    public const string WeaponFrontAnchor = "weaponFrontAnchor";
    public const string WeaponBackAnchor = "weaponBackAnchor";
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