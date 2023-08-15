using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fog : MonoBehaviour
{
    float left = -6f;
    float right = 6f;
    Vector2 yRange = new Vector2(-1.75f,2.5f);
    public float speed = 1f;
    // Start is called before the first frame update
    void Start()
    {
        SetUp();
        transform.position = new Vector2(Random.Range(left,right),transform.position.y);
    }
    void SetUp(){
        transform.position = new Vector2(left,Random.Range(yRange.x,yRange.y));
    }

    // Update is called once per frame
    void Update()
    {
        transform.position+=Vector3.right*speed;
        if(transform.position.x >= right){
            SetUp();
        }
    }
}
