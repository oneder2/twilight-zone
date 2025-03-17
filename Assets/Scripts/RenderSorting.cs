using UnityEngine;

public class RenderSorting : MonoBehaviour
{
    // Render static object for layer
    void Start()
    {
        GetComponent<Renderer>().sortingOrder = -(int)transform.position.y;
    }
}
