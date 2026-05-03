using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Vehicle/PrefabRegistry")]
public class VehiclePrefabRegistry : ScriptableObject
{


    [SerializeField]
    public VehicleElement EmptyPart;

    [SerializeField] private List<VehicleEntry> baseElements;
    [SerializeField] private List<VehicleEntry> bodyElements;
    [SerializeField] private List<VehicleWeaponEntry> weapons;

    private Dictionary<string, VehicleEntry> _baseMap;
    private Dictionary<string, VehicleEntry> _bodyMap;
    private Dictionary<string, VehicleWeaponEntry> _weaponMap;

    private void OnEnable()
    {
        _baseMap = baseElements.ToDictionary(e => e.key, e => e);
        _bodyMap = bodyElements.ToDictionary(e => e.key, e => e);
        _weaponMap = weapons.ToDictionary(e => e.key, e => e);
    }

    public List<VehicleEntry> GetAllBases() => baseElements;

    public List<VehicleEntry> GetAllBodies() => bodyElements;

    public List<VehicleWeaponEntry> GetAllWeapons() => weapons;

    public VehicleEntry GetBase(string key) => _baseMap.TryGetValue(key, out var v) ? v : null;
    public VehicleEntry GetBody(string key) => _bodyMap.TryGetValue(key, out var v) ? v : null;
    public VehicleEntry GetWeapon(string key) => _weaponMap.TryGetValue(key, out var v) ? v : null;
}