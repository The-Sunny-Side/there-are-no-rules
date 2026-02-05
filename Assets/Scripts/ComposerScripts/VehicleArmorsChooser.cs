using UnityEngine;

public class VehicleArmorsChooser : VehicleElementChooser
{
    // 0 = Top - 1 = Left - 2 = Back - 3 = Right 
    public int selectedArmorType = 0;
    public GameObject[] selectedArmors;

    private GameObject currentInstance;
    private int currentIndex = 0;

    private void Awake()
    {
        GameObject emptyObj = Resources.Load<GameObject>("empty");
        selectedArmors = new GameObject[] { emptyObj, emptyObj, emptyObj, emptyObj };
    }

    public new void NextElement()
    {
        int nextIndex = (currentIndex + 1) % this.elements.Length;

        if (currentInstance != null)
        {
            Destroy(currentInstance);
        }
        currentIndex = nextIndex;
        currentInstance = Instantiate(elements[nextIndex], selectedElement.transform);
    }
    public new void PreviousElement()
    {
        int previousIndex = (currentIndex - 1 + elements.Length) % elements.Length;

        if (currentInstance != null)
        {
            Destroy(currentInstance);
        }
        currentIndex = previousIndex;
        currentInstance = Instantiate(elements[previousIndex], selectedElement.transform);
    }

    public void NextArmorType()
    {
        selectedArmorType = (selectedArmorType + 1 + 4) % 4;
    }

    public void PreviousArmorType()
    {
        selectedArmorType = (selectedArmorType + 1 + 4) % 4;
    }

    public void SetArmorType(int type)
    {
        selectedArmorType = type;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            NextElement();
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            PreviousElement();
        }
    }
}
