using PurrNet.Prediction;
using System.Collections.Generic;
using UnityEngine;

public class PredictedAttacks : PredictedIdentity<PredictedAttacks.Input, PredictedAttacks.State>
{
    private MobileInputManager _inputManager;
    private Dictionary<string, string> weaponsKey = new Dictionary<string, string>();

    protected override void LateAwake()
    {
        _inputManager = MobileInputManager.instance;
        weaponsKey.Add("front", AnchorNames.WeaponFrontAnchor);
        weaponsKey.Add("back", AnchorNames.WeaponBackAnchor);
        weaponsKey.Add("left", AnchorNames.WeaponLeftAnchor);
        weaponsKey.Add("right", AnchorNames.WeaponRightAnchor);

    }
    protected override void UpdateInput(ref Input input)
    {

        input.useFront = _inputManager.slideUpHeld;
        input.useBack= _inputManager.slideDownHeld;
        input.useRight = _inputManager.slideRightHeld;
        input.useLeft=_inputManager.slideLeftHeld;

    }
    public struct Input : IPredictedData
    {
        public bool useFront;
        public bool useBack;
        public bool useRight;
        public bool useLeft;
        public void Dispose() { }
    }
    public struct State : IPredictedData<State>
    {
        public void Dispose() { }
    }

    protected override void Simulate(Input input, ref State state, float delta)
    {
        if (input.useFront)
        {
            ActivateWeapon("front");
        }

        if (input.useBack)
        {
            ActivateWeapon("back");
        }

        if (input.useLeft)
        {
            ActivateWeapon("left");
        }

        if (input.useRight)
        {
            ActivateWeapon("right");
        }
        
}
    public void ActivateWeapon(string direction)
    {
        if (weaponsKey.TryGetValue(direction, out string partName))
        {
            VehicleWeapon weapon = gameObject.FindChildWithName(partName)?.FindChildWithTag("Weapon")?.GetComponent<VehicleWeapon>();
            if (weapon != null)
            {
                weapon.ActivateWeapon();
            }
        }
    }

}
