using PurrNet.Prediction;
using System.Collections.Generic;
using UnityEngine;

public class PredictedAttacks : PredictedIdentity<PredictedAttacks.Input, PredictedAttacks.State>
{
    private IVehicleInputProvider _input;
    private SwipeDetector _swipeDetector;
    private Dictionary<string, WeaponHud> weaponsKey = new Dictionary<string, WeaponHud>();

    protected override void LateAwake()
    {
        _input = GetComponent<IVehicleInputProvider>();

        weaponsKey = VehicleManager.Instance.GetWeaponsHud();
    }

    protected override void UpdateInput(ref Input input)
    {
        input.useFront = _input.UseFront;
        input.useBack = _input.UseBack;
        input.useRight = _input.UseRight;
        input.useLeft = _input.UseLeft;
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
        if (weaponsKey.TryGetValue(direction, out WeaponHud weaponHud))
        {
            VehicleWeapon weapon = gameObject.FindChildWithName(weaponHud?.weaponPart)?.FindChildWithTag("Weapon")?.GetComponent<VehicleWeapon>();

            if (!weaponHud.sliceFill.IsFilling && weapon != null && !weapon.isEmpty)
            {
                Debug.Log("Triggering weapon: " + direction);
                weaponHud.sliceFill.StartCooldown();
                weapon.ActivateWeapon();
            }
        }
    }

}
