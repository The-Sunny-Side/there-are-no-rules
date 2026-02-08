using UnityEngine;
using UnityEngine.UI;

public class VehicleArmorsChooser : VehicleElementChooser
{
    // 0 = Top - 1 = Left - 2 = Back - 3 = Right 
    public int selectedArmorType = 0;
    public GameObject[] selectedArmors;
    private int[] selectedArmorsIndices = new int[4] { 0, 0, 0, 0 };
    [SerializeField] private Sprite[] icons;

    [SerializeField] private Composer composer;

    [SerializeField] private Image[] iconBoxes;


    public override void NextElement()
    {
        
        int nextIndex = (currentIndex + 1) % this.elements.Length;
        currentIndex = nextIndex;
        selectedArmors[selectedArmorType] = elements[nextIndex];
        selectedArmorsIndices[selectedArmorType]= nextIndex;
        SyncSelectedElement(elements[nextIndex]);
    }
    public override void PreviousElement()
    {

        int previousIndex = (currentIndex - 1 + elements.Length) % elements.Length;
        currentIndex = previousIndex;
        selectedArmors[selectedArmorType] = elements[previousIndex];
        selectedArmorsIndices[selectedArmorType] = previousIndex;
        SyncSelectedElement(elements[previousIndex]);
    }

    public void NextArmorType()
    {
        selectedArmorType = (selectedArmorType + 1 + 4) % 4;
        currentIndex = selectedArmorsIndices[selectedArmorType];
        SyncSelectedElement(elements[currentIndex]);
    }

    public void PreviousArmorType()
    {
        selectedArmorType = (selectedArmorType - 1 + 4) % 4;
        currentIndex = selectedArmorsIndices[selectedArmorType];
        SyncSelectedElement(elements[currentIndex]);
    }

    public void SetArmorType(int type)
    {
        selectedArmorType = type;
        currentIndex = selectedArmorsIndices[selectedArmorType];
        SyncSelectedElement(elements[currentIndex]);
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

    protected void SyncSelectedElement(GameObject element)
    {
        Debug.Log("syncing element: " + element.name + " for armor type: " + selectedArmorType);
        selectedElement = element;
        iconBoxes[selectedArmorType].sprite = icons[currentIndex];
        composer.armorFrontElement = Instantiate(selectedArmors[0], transform);
        composer.armorLeftElement = Instantiate(selectedArmors[1], transform);
        composer.armorBackElement = Instantiate(selectedArmors[2], transform);
        composer.armorRightElement = Instantiate(selectedArmors[3], transform);

        composer.AlignComponents();
    }
}
