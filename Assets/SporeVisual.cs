using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Shapes;

public class SporeVisual : MonoBehaviour
{
    public int pointsNum;
    public float scan;
    public float height;
    public float speed;
    public List<Polyline> lines;
    public List<float> offsets;
    // Start is called before the first frame update
    void Start()
    {
        for(int j = 0; j < lines.Count;j++){
            var line = lines[j];
            line.points.Clear();
            for(var i = 0; i < pointsNum;i++){
                float x = (1.0f/(float)(pointsNum-1)) * (float)i;
                line.AddPoint(new Vector3(x,offsets[j]+Mathf.Sin(x*scan)*height,0f));
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        for(int j = 0; j < lines.Count;j++){
            var line = lines[j];
            for(var i = 0; i < pointsNum;i++){
                float x = (1.0f/(float)(pointsNum-1)) * (float)i;
                float test_x = x+transform.position.x;
                line.SetPointPosition(i,new Vector3(x,offsets[j]+Mathf.Sin((test_x+Time.time*speed)*scan)*height,0f));
            }
        }
    }
}
