using System.Collections;
using Lattice;
using UnityEngine;

public class LatticeHitTrigger : MonoBehaviour
{
    [Header("Lattice da spawnare")]
    [SerializeField] public GameObject lattice;

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("hit");
        GameObject hitted = collision.gameObject;
        if (collision.gameObject.tag == "Deformable")
        {
            Debug.Log("deformable");
            var modifier=hitted.GetComponent<LatticeModifier>();
            if (modifier == null)
            {
                Debug.LogWarning("Nessun LatticeModifier trovato sul bersaglio!");
                return;
            }
            foreach (var contact in collision.contacts)
            {
                Debug.Log($"Impatto in {contact.point} con normale {contact.normal}");
                Vector3 hitPoint = contact.point;
                Vector3 hitDir = -contact.normal;
                GameObject instantiatedLattice = Instantiate(lattice, hitPoint, Quaternion.LookRotation(hitDir));
                
                var latticeItem = new LatticeItem()
                {
                    Lattice = instantiatedLattice.GetComponent<Lattice.Lattice>(),
                    Interpolation = InterpolationMethod.Cubic,
                    Global = false,
                };

                modifier.Lattices.Add(latticeItem);
                StartCoroutine(RemoveLatticeAfterDelay(modifier, latticeItem, instantiatedLattice, 2f));

            }
        }
    }
    private IEnumerator RemoveLatticeAfterDelay(LatticeModifier modifier, LatticeItem item, GameObject latticeObj, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (modifier != null)
        {
            modifier.Lattices.Remove(item);

            modifier.Lattices.RemoveAll(l => l.Lattice == null);
        }
    }

}
