using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomPlacement : MonoBehaviour
{
    public Vector2 lowest;
    public Vector2 highest;
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("B");
        transform.localPosition= new Vector3(Random.Range(lowest.x,highest.x),Random.Range(lowest.y,highest.y),0f);
        Destroy(this);
    }
}
