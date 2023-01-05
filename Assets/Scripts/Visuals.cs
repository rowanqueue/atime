﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Visuals : MonoBehaviour
{
    public float edgeWidth = 0.2f;//thick wall
    public float walkAbleEdgeWidth = 0.1f;
    public float shortenWalls = 0.05f;
    [SerializeField]
    float lerpFloat = 0.1f;
    public float lerpSpeed => lerpFloat*(Time.deltaTime/0.016f);//*Mathf.Clamp(Time.deltaTime/0.016f,0.01f,2f);
    public Color playerColor;
    public Color cloneColor;
    public Color tileColor;
    public Color tinyTileColor;
    public Color winColor;
    public Color actionColor;
    public float levelSelectShrink;

    public Vector2 LerpVector(Vector2 start, Vector2 finish){
        if(Vector2.Distance(start,finish) < 0.01f){return finish;}
        return start += (finish-start)*lerpSpeed;
    }
    public Vector3 LerpVector(Vector3 start, Vector2 finish){
        if(Vector3.Distance(start,finish) < 0.01f){return finish;}
        return start += ((Vector3)finish-start)*lerpSpeed;
    }
    public Vector3 LerpVector(Vector3 start, Vector3 finish){
        if(Vector3.Distance(start,finish) < 0.01f){return finish;}
        return start += (finish-start)*lerpSpeed;
    }
}