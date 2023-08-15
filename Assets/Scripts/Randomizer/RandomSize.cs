using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomSize : MonoBehaviour
{
    public Vector2 range = Vector2.one;
    // Start is called before the first frame update
    void Start()
    {
        float scale = Random.Range(range.x,range.y);
        transform.localScale= Vector3.one*scale;
        Destroy(this);
    }
}
