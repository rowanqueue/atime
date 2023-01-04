using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionPoint
{
    
    public bool on = true;
    public bool collected = false;//its a part of the turn structure now
    public bool held = false;//its being held by a player
    public Player playerHolding;
    public Vector2Int gridPosition;
    public int index;
    LineRenderer circle;
    float outerWidth;
    float innerWidth;
    public GameObject gameObject;
    public bool startedAsTurn;

    public ActionPoint(Vector2Int gridPosition,Transform parent){//starts in the level
        this.gridPosition = gridPosition;
        gameObject = GameObject.Instantiate(Services.GameController.actionPointPrefab,(Vector2)gridPosition,Quaternion.identity,parent);
        circle = gameObject.transform.GetChild(0).GetComponent<LineRenderer>();
        outerWidth = circle.startWidth;
        index = -1;
    }
    public ActionPoint(int index, Transform parent){//starts already had
        startedAsTurn = true;
        this.index = index;
        collected = true;
        gameObject = GameObject.Instantiate(Services.GameController.actionPointPrefab,(Vector2)gridPosition,Quaternion.identity,parent);
        circle = gameObject.transform.GetChild(0).GetComponent<LineRenderer>();
        outerWidth = circle.startWidth;
    }
    //
    public void Grab(Player player){
        held = true;
        playerHolding = player;
    }
    public void UnGrab(){
        held = false;
        playerHolding = null;
    }
    public void Collect(){
        held = false;
        Services.Grid.actionPoints.Remove(gridPosition);
        collected = true;
        index = Services.GameController.turnLimitDisplay.Count;
        gameObject.transform.parent = Services.GameController.turnLimitParent;
        Services.GameController.turnLimitDisplay.Add(this);
    }
    public void UndoCollect(){
        on = true;
        Services.Grid.actionPoints.Add(gridPosition,this);
        collected = false;
        gameObject.transform.parent = Services.Grid.level.gameObject.transform;
        Services.GameController.turnLimitDisplay.Remove(this);
    }
    public void Draw()
    {
        if(collected){
            circle.startColor = on ? Services.Visuals.actionColor : Services.Visuals.tileColor;
            
            //circle.startWidth+=(0.2f-circle.startWidth)*Services.Visuals.lerpSpeed;
            //circle.SetPosition(1,Services.Visuals.LerpVector(circle.GetPosition(1),Vector2.right*0.5f));
        }else{
            circle.startColor = Services.Visuals.actionColor;
            circle.startWidth = outerWidth*gameObject.transform.parent.localScale.x;
            circle.SetPosition(1,Services.Visuals.LerpVector(circle.GetPosition(1),Vector2.zero));
        }
        circle.endWidth = circle.startWidth;
        circle.endColor = circle.startColor;
        if(held){
            gameObject.transform.localPosition = Services.Visuals.LerpVector(gameObject.transform.localPosition,(Vector2)playerHolding.position+Vector2.up*0.5f);
            return;
        }
        if(collected){
            gameObject.transform.localPosition += (new Vector3(index*0.5f,0,0)-gameObject.transform.localPosition)*Services.Visuals.lerpSpeed;
        }else{
            gameObject.transform.localPosition += ((Vector3)(Vector2)gridPosition-gameObject.transform.localPosition)*Services.Visuals.lerpSpeed;
        }
        
    }
    public void InstantShrink(){
        float targetWidth = innerWidth*gameObject.transform.parent.localScale.x;
        if(!on){
            targetWidth = 0;
        }
        circle.startWidth = outerWidth*gameObject.transform.parent.localScale.x;
        circle.endWidth = circle.startWidth;
        if(collected){
            gameObject.transform.localPosition += (new Vector3(index,0,0)-gameObject.transform.localPosition)*Services.Visuals.lerpSpeed;
        }else{
            gameObject.transform.localPosition = (Vector2)gridPosition;
        }
    }
    public void Show(){
        circle.enabled = true;
    }
    public void Hide(){
        circle.enabled = false;
    }
    //only for level editing
    public void Destroy(){
        GameObject.Destroy(gameObject);
    }
}
public class ActionPointState
{
    public bool held;
    public int playerHoldingIndex;
    public ActionPointState(ActionPoint ap){
        held = ap.held;
        if(held){
            playerHoldingIndex = ap.playerHolding.index;
        }
        
    }
}
