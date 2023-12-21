using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Shapes;

public class ActionPoint
{
    public ActionPointView view;
    public bool on = true;
    public bool collected = false;//its a part of the turn structure now
    public bool held = false;//its being held by a player
    public Player playerHolding;
    public Vector2Int gridPosition;
    public int index;
    //how powerful it is
    public int num = 1;
    
    LineRenderer circle;
    TextMeshPro text;
    float outerWidth;
    float innerWidth;
    public GameObject gameObject;
    public bool startedAsTurn;
    public bool createdMidGame;
    public int timer = 0;
    public List<LineRenderer> petals;
    public SpriteRenderer leftBehind;
    public List<SpriteRenderer> sideFlowers = new List<SpriteRenderer>();
    public float flowerIndex;
    public SpriteRenderer orb;

    /// <summary>
    /// Use when starting in level
    /// </summary>
    /// <param name="gridPosition"></param>
    /// <param name="parent"></param>
    public ActionPoint(Vector2Int gridPosition,Transform parent)
    {
        this.gridPosition = gridPosition;
        gameObject = GameObject.Instantiate(Services.GameController.actionPointPrefab,(Vector2)gridPosition,Quaternion.identity,parent);
        view = gameObject.transform.GetComponent<ActionPointView>();
        view.InitializeView(ActionPointStates.InLevel);

        circle = gameObject.transform.GetChild(0).GetComponent<LineRenderer>();
        outerWidth = circle.startWidth;
        index = -1;
        text = gameObject.GetComponentInChildren<TextMeshPro>();
        petals = new List<LineRenderer>();
        foreach(Transform child in circle.transform){
            petals.Add(child.GetComponent<LineRenderer>());
        }
        leftBehind = gameObject.transform.GetChild(1).GetComponent<SpriteRenderer>();
        leftBehind.transform.parent = gameObject.transform.parent;
        leftBehind.transform.localPosition = (Vector3)(Vector2)gridPosition+new Vector3(1f,1f,0f)*0.5f;
        sideFlowers = new List<SpriteRenderer>();
        foreach(Transform child in leftBehind.transform){
            sideFlowers.Add(child.GetComponent<SpriteRenderer>());
            if(Random.value < 0.15f){
                child.GetComponent<SpriteRenderer>().enabled = false;
            }else{
                if(Random.value < 0.5f){
                    child.GetComponent<SpriteRenderer>().flipX = true;
                }
            }
        }
        orb = gameObject.transform.GetChild(2).GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// Use when point starts in UI
    /// </summary>
    /// <param name="index"></param>
    /// <param name="parent"></param>
    public ActionPoint(int index, Transform parent)
    {
        startedAsTurn = true;
        this.index = index;
        collected = true;
        gameObject = GameObject.Instantiate(Services.GameController.actionPointPrefab,(Vector2)gridPosition,Quaternion.identity,parent);
        view = gameObject.transform.GetComponent<ActionPointView>();
        view.InitializeView(ActionPointStates.InUI);
        circle = gameObject.transform.GetChild(0).GetComponent<LineRenderer>();
        outerWidth = circle.startWidth;
        text = gameObject.GetComponentInChildren<TextMeshPro>();
        leftBehind = gameObject.transform.GetChild(1).GetComponent<SpriteRenderer>();
        orb = gameObject.transform.GetChild(3).GetComponent<SpriteRenderer>();
        //get rid of leftbehind
        leftBehind.gameObject.SetActive(false);
    }

    /// <summary>
    /// Use when created by multi action points in the UI???
    /// </summary>
    /// <param name="parent"></param>
    public ActionPoint(Transform parent)
    {
        

        createdMidGame = true;
        gameObject = GameObject.Instantiate(Services.GameController.actionPointPrefab,(Vector2)gridPosition,Quaternion.identity,parent);
        view = gameObject.transform.GetComponent<ActionPointView>();
        view.InitializeView(ActionPointStates.InUI);
        circle = gameObject.transform.GetChild(0).GetComponent<LineRenderer>();
        outerWidth = circle.startWidth;
        index = -1;
        text = gameObject.GetComponentInChildren<TextMeshPro>();
        leftBehind = gameObject.transform.GetChild(1).GetComponent<SpriteRenderer>();
        orb = gameObject.transform.GetChild(3).GetComponent<SpriteRenderer>();
        //Get rid of leftbehind
        leftBehind.gameObject.SetActive(false);
    }

    public bool CanGrab(){
        if(Services.GameController.currentTurn >= timer){
            return true;
        }else{
            return false;
        }
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
        Services.GameController.turnLimit++;
        if(num > 1){
            int n = num;
            while(n > 1){
                ActionPoint newActionPoint = new ActionPoint(gameObject.transform.parent);
                newActionPoint.Collect();
                n-=1;
            }
        }
    }

    public void UndoCollect(){
        on = true;
        if(createdMidGame){
            GameObject.Destroy(this.gameObject);
        }else{
            Services.Grid.actionPoints.Add(gridPosition,this);
            collected = false;
            gameObject.transform.parent = Services.Grid.level.gameObject.transform;
        }
        
        
        Services.GameController.turnLimitDisplay.Remove(this);
        Services.GameController.turnLimit--;
        
    }

    public void Draw()
    {
        if(startedAsTurn == false && createdMidGame == false){
            int turns_left = timer-Services.GameController.currentTurn;
            //TODO: when we figure out how to acutally display timers probably want it visbile in level select
            if(collected || Services.GameController.state == GameState.LevelSelect){
                turns_left = 0;
            }
            if(turns_left<0){
                turns_left = 0;
            }
            if(turns_left == 0){
                foreach(LineRenderer petal in petals){
                    petal.SetPosition(1,Vector3.zero);
                    petal.enabled = false;
                }
            }else{
                while(turns_left > petals.Count){
                    GameObject newPetal = GameObject.Instantiate(petals[0].gameObject,Vector3.zero,Quaternion.identity,petals[0].transform.parent) as GameObject;
                    newPetal.transform.localPosition = Vector3.zero;
                    petals.Add(newPetal.GetComponent<LineRenderer>());
                }
                float fraction = (2f*Mathf.PI)/(float)timer;
                float offset = Mathf.PI*0.5f;
                float _radius = 0.2f;
                for(int i = 0; i < petals.Count;i++){
                    petals[i].enabled = true;
                    Vector3 targetPosition = Vector3.zero;
                    if(i < turns_left){
                        targetPosition = new Vector3(Mathf.Cos(offset+(-i*fraction)),Mathf.Sin(offset+(-i*fraction)))*_radius;
                    }
                    petals[i].SetPosition(1,petals[i].GetPosition(1)+(targetPosition-petals[i].GetPosition(1))*0.1f);
                }
            }
        }
        
        if(num < 2 || collected){
            text.text = "";
        }else{
            text.text = num.ToString();
        }
        if(collected){
            circle.startColor = on ? Services.Visuals.actionColor : Services.Visuals.tileColor;
            
            //circle.startWidth+=(0.2f-circle.startWidth)*Services.Visuals.lerpSpeed;
            //circle.SetPosition(1,Services.Visuals.LerpVector(circle.GetPosition(1),Vector2.right*0.5f));
        }else{
            circle.startColor = Services.Visuals.actionColor;
            circle.startWidth = outerWidth*gameObject.transform.parent.localScale.x;
            circle.SetPosition(1,Services.Visuals.LerpVector(circle.GetPosition(1),Vector2.zero));
        }
        
        if(held){
            Vector2 targetPosition = Vector2.zero;
            targetPosition.y = -0.15f+Mathf.Cos(Time.time*3f)*0.04f;
            circle.startWidth = outerWidth*gameObject.transform.parent.localScale.x*1.2f;
            gameObject.transform.localPosition = Services.Visuals.LerpVector(gameObject.transform.localPosition,(Vector2)playerHolding.position+targetPosition);
            circle.sortingLayerName = "Player";
            circle.sortingOrder = 5;
        }else{
            circle.sortingLayerName = "Point";
            circle.sortingOrder = 1;
            if(collected){
                gameObject.transform.localPosition += (new Vector3(index*0.5f,0,0) - gameObject.transform.localPosition)*Services.Visuals.lerpSpeed;
            }else{
                gameObject.transform.localPosition += ((Vector3)(Vector2)gridPosition - gameObject.transform.localPosition) * Services.Visuals.lerpSpeed;
            }
        }
        circle.endWidth = circle.startWidth;
        circle.endColor = circle.startColor;

        if(collected == false){
            flowerIndex += Time.deltaTime * 10f * Random.Range(0f,2f);
            int flooredIndex = Mathf.FloorToInt(flowerIndex);
            if(flooredIndex >= Services.Visuals.flowerSprites.Count){
                flowerIndex =Services.Visuals.flowerSprites.Count-1;
                flooredIndex = Mathf.FloorToInt(flowerIndex);
            }
            leftBehind.sprite = Services.Visuals.flowerSprites[flooredIndex];
            for(int i = 0; i < sideFlowers.Count;i++){
                int this_index = flooredIndex;
                if(flooredIndex >= Services.Visuals.sideFlowerSprites[i].Count){
                    this_index = Services.Visuals.sideFlowerSprites[i].Count-1;
                }
                sideFlowers[i].sprite = Services.Visuals.sideFlowerSprites[i][this_index];
            }
            if(flooredIndex < 12){
                orb.enabled = false;
            }else{
                orb.enabled = true;
            }
        }else{
            leftBehind.sprite = Services.Visuals.flowerSprites[0];
            for(int i = 0; i < sideFlowers.Count;i++){
                sideFlowers[i].sprite = Services.Visuals.sideFlowerSprites[i][0];
            }
            orb.enabled = true;
            orb.sortingLayerName = "UI";
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
        GameObject.Destroy(leftBehind.gameObject);
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
