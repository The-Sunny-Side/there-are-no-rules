using UnityEngine;

public class VehicleElementChooser : MonoBehaviour
{
    [SerializeField] public GameObject[] elements;
    public GameObject selectedElement;
    protected GameObject currentInstance;
    protected int currentIndex = 0;

    void Awake()
    {
        if (elements.Length > 0)
            selectedElement = elements[0];
    }
    protected void Start()
    {
        if (selectedElement != null)
        {
            currentInstance = Instantiate(selectedElement, transform);
        }
    }
    public virtual void NextElement()
    {
        currentIndex = System.Array.IndexOf(elements, selectedElement);
        int nextIndex = (currentIndex + 1) % elements.Length;
        selectedElement = elements[nextIndex];


        Destroy(currentInstance);
        currentInstance = Instantiate(selectedElement, transform);
    }
    public virtual void PreviousElement()
    {
        currentIndex = System.Array.IndexOf(elements, selectedElement);
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
