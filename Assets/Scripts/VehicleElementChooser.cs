using UnityEngine;

public class VehicleElementChooser : MonoBehaviour
{
    [SerializeField] private GameObject[] elements;
    public GameObject selectedElement;
    private GameObject currentInstance;
    void Awake()
    {
        if (elements.Length > 0)
            selectedElement = elements[0];
    }
    void Start()
    {
        if (selectedElement != null)
        {
            currentInstance = Instantiate(selectedElement, transform);
        }
    }
    public void NextElement()
    {
        Debug.Log("next element from chooser");
        int currentIndex = System.Array.IndexOf(elements, selectedElement);
        int nextIndex = (currentIndex + 1) % elements.Length;
        selectedElement = elements[nextIndex];


        Destroy(currentInstance);
        currentInstance = Instantiate(selectedElement, transform);
    }
    public void PreviousElement()
    {
        int currentIndex = System.Array.IndexOf(elements, selectedElement);
        int previousIndex = (currentIndex - 1 + elements.Length) % elements.Length;
        selectedElement = elements[previousIndex];

        Destroy(currentInstance);
        currentInstance = Instantiate(selectedElement, transform);
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
