using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineChangeColor : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        LineRenderer line = GetComponent<LineRenderer>();
        line.startColor = Services.Visuals.tileColor;
        line.endColor = line.startColor;
        Destroy(this);
    }

}
