using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Shapes;

public class Exit
{
    public bool on = false;
    public Vector2Int position;
    LineRenderer line;
    SpriteRenderer fill;
    SpriteRenderer spiral;
    Disc spiralCenter;
    float lineWidth;

    public GameObject gameObject;
    public Level level;

    public Exit(Vector2Int position,Transform parent){
        this.position = position;
        gameObject = GameObject.Instantiate(Services.GameController.exitPrefab,(Vector2)position,Quaternion.identity,parent);
        gameObject.transform.localPosition = (Vector2)position;
        line = gameObject.transform.GetChild(0).GetChild(0).GetComponent<LineRenderer>();
        line.startColor = Services.Visuals.winColor;
        line.endColor = Services.Visuals.winColor;
        lineWidth = line.startWidth;
        fill = gameObject.transform.GetChild(0).GetChild(1).GetComponent<SpriteRenderer>();
        fill.color = line.startColor;
        spiral = gameObject.transform.GetChild(0).GetChild(2).GetComponent<SpriteRenderer>();
        spiral.color = line.startColor;
        spiralCenter = gameObject.transform.GetComponentInChildren<Disc>();
        spiralCenter.Color = spiral.color;
    }
    //press it
    public void TurnOn(){
        if(on == false){
            fill.transform.localScale = Vector3.one*5f;
        }
        on = true;
        line.sortingLayerName = "Tile";
        line.sortingOrder = -10;
        fill.sortingLayerName = "Tile";
        fill.sortingOrder = -10;
        
    }
    //undo pressing it OR start a new level
    public void TurnOff(){
        fill.sortingLayerName = "Point";
        fill.sortingOrder = 0;
        line.enabled = true;
        on = false;
    }
    public void InstantShrink(){
        fill.transform.localScale = Vector3.one*10f;

    }
    //only for level editing
    public void Destroy(){
        GameObject.Destroy(gameObject);
    }
    public void SetPosition(Vector2Int pos){
        if(pos == new Vector2Int(-1,-1)){
            Destroy();
            return;
        }
        position = pos;
        gameObject.transform.localPosition = (Vector2)pos;
    }

    public void Draw(){
        Color targetColor = Services.Visuals.winColor;
        bool spinning = false;
        if(Services.GameController.state == GameState.LevelSelect){
            on = true;
            fill.sortingLayerName = "Tile";
            fill.sortingOrder = -10;
            line.enabled = false;
            if(Services.LevelSelect.won[level.index] == false){
                targetColor = Services.Visuals.tileColor;
            }
        }else{
            spiral.transform.eulerAngles += new Vector3(0,0,1f*(Time.deltaTime/0.016f));
            if(level != Services.Grid.level){
                targetColor = Services.Visuals.tileColor;
            }
            if(Services.GameController.noLimit == false && Services.LevelSelect.won[level.index] == false && Services.GameController.currentTurn >= Services.GameController.turnLimit){
                targetColor = Services.Visuals.tileColor;
            }else{
                spinning = true;
                
            }
        }
        
        /*float targetAngle = on ? 45f : 0f;
        gameObject.transform.GetChild(0).localEulerAngles = Services.Visuals.LerpVector(gameObject.transform.GetChild(0).localEulerAngles,new Vector3(0,0,targetAngle));*/
        line.transform.localScale = Services.Visuals.LerpVector(line.transform.localScale,on ? Vector3.one*1.9f : Vector3.one);
        line.startWidth = lineWidth*gameObject.transform.parent.localScale.x;
        line.startColor = targetColor;//on ? Services.Visuals.winColor : Services.Visuals.tileColor;
        line.endColor = line.startColor;
        line.endWidth = line.startWidth;
        fill.color = targetColor;
        fill.transform.localScale = Services.Visuals.LerpVector(fill.transform.localScale,on ? Vector3.one*10f : Vector3.one);
        spiral.color = targetColor;
        spiralCenter.Color = targetColor;
        spiral.transform.localScale = Services.Visuals.LerpVector(spiral.transform.localScale,Vector3.one*(spinning ? 0.75f:0.4f));
    }
    public void FollowMouse(Vector2 pos){
        Vector3 target = (Vector3)(pos-Vector2.one*0.5f);
        target.x += 0.1f*Mathf.Sin(Time.time*5f);
        target.y += 0.1f*Mathf.Cos(Time.time*3f);
        gameObject.transform.position = target;
    }
}
