using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile
{
    public Vector2Int position;
    //neighbor stuff
    public Tile[] neighbors = new Tile[4];
    public bool[] canMove = new bool[4]{true,true,true,true};
    public bool[] walls = new bool[4]{false,false,false,false};
    public bool[] spikes = new bool[4]{false,false,false,false};
    LineRenderer[] edges = new LineRenderer[4];
    GameObject gameObject;

    public bool CanWalk => !hasPit || (hasPit && pitFilled);
    public bool hasPit = false;
    public bool pitFilled = false;
    GameObject pit;

    public Tile(Vector2Int position,Transform parent){
        this.position = position;
        gameObject = GameObject.Instantiate(Services.GameController.tilePrefab,(Vector2)position,Quaternion.identity,parent);
        gameObject.transform.localPosition = (Vector2)position;
        for(var i = 0; i < neighbors.Length;i++){
            edges[i] = gameObject.transform.GetChild(i).GetComponent<LineRenderer>();
        }
    }
    //only for level editing
    public void AddPit(){
        hasPit = true;
        pit = GameObject.Instantiate(Services.GameController.pitPrefab,gameObject.transform);
    }
    public void RemovePit(){
        hasPit = false;
        GameObject.Destroy(pit);
        pit = null;
    }
    public void FillPit(){
        pitFilled = true;
    }
    public void UnFillPit(){
        pitFilled = false;
    }
    public void Destroy(){
        GameObject.Destroy(gameObject);
    }
    //end level editing!
    public void SetPosition(Vector2Int pos){
        position = pos;
        gameObject.transform.localPosition = (Vector2)pos;
    }

    public void Draw(bool won){
        if(hasPit){
            pit.transform.GetChild(1).gameObject.SetActive(pitFilled);
        }
        for(var i = 0; i < edges.Length;i++){
            edges[i].startColor = won && Services.GameController.state == GameState.LevelSelect ? Services.Visuals.winColor : (canMove[i] ? Services.Visuals.tinyTileColor : Services.Visuals.tileColor);
            edges[i].sortingOrder = (canMove[i] ? 0 : 1);
            edges[i].endColor = edges[i].startColor;
            if(canMove[i]){
                edges[i].startWidth = Services.Visuals.walkAbleEdgeWidth*gameObject.transform.parent.localScale.x;
            }else{
                edges[i].startWidth = Services.Visuals.edgeWidth*gameObject.transform.parent.localScale.x;
            }
            if(walls[i]){
                if(spikes[i]){
                    edges[i].loop = true;
                }else{
                    edges[i].loop = false;
                }
                bool horizontal = i == 0 || i == 2;
                Vector2 corner = Services.Grid.corners[(i-1+4)%4];
                if(horizontal){
                    if(corner.x < 0.5f){
                        corner.x+=Services.Visuals.shortenWalls;
                    }else{
                        corner.x*=(1f-Services.Visuals.shortenWalls);
                    }
                }else{
                    if(corner.y < 0.5f){
                        corner.y+=Services.Visuals.shortenWalls;
                    }else{
                        corner.y*=(1f-Services.Visuals.shortenWalls);
                    }
                }
                
                
                edges[i].SetPosition(0,corner);
                corner = Services.Grid.corners[i];
                if(horizontal){
                    if(corner.x < 0.5f){
                        corner.x+=Services.Visuals.shortenWalls;
                    }else{
                        corner.x*=(1f-Services.Visuals.shortenWalls);
                    }
                }else{
                    if(corner.y < 0.5f){
                        corner.y+=Services.Visuals.shortenWalls;
                    }else{
                        corner.y*=(1f-Services.Visuals.shortenWalls);
                    }
                }
                edges[i].SetPosition(1,corner);
            }
            
            
            edges[i].endWidth = edges[i].startWidth;
        }
    }
}
