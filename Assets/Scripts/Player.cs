using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Shapes;

public class Player
{
    public Vector2Int spawnPos;
    public Vector2Int position;
    public bool isMoving;
    public bool spawning;
    public bool completedLoop;
    public int index;
    public int failureDirection = -1;
    public int waitingStatus;//0: not waiting, 1: turning
    public bool sittingDown;
    public bool changingSitting;
    public bool waitToMoveToUndoSitting;
    float timeTurnStarted;

    public List<int> moves = new List<int>();
    
    GameObject gameObject;
    Disc head;
    RegularPolygon body;
    Rectangle[] legs = new Rectangle[2];
    GameObject noseCenter;
    RegularPolygon nose;

    TextMeshPro cloneNumber;
    float[] lineWidths = new float[2];
    Player pusher;//player pushing me
    public bool winning;
    public bool dead;
    public bool inPit;
    public SpriteRenderer spriteRenderer;
    public float animIndex;
    public bool eating;
    public bool actuallyEating;
    public SpriteRenderer poof;

    public Player(Vector2Int pos, Transform parent){
        Debug.Log(pos);
        spawnPos = pos;
        index = 0;//Services.GameController.players.Count;
        position = pos;
        gameObject = GameObject.Instantiate(Services.GameController.playerPrefab,(Vector2)position,Quaternion.identity,parent);
        gameObject.transform.localPosition = (Vector2)position + Vector2.one*0.5f;
        head = gameObject.GetComponentInChildren<Disc>();
        body = gameObject.GetComponentInChildren<RegularPolygon>();
        legs[0] = body.transform.GetChild(1).GetComponent<Rectangle>();
        legs[1] = body.transform.GetChild(2).GetComponent<Rectangle>();
        cloneNumber = gameObject.GetComponentInChildren<TextMeshPro>();
        noseCenter = head.transform.GetChild(0).gameObject;
        nose = noseCenter.GetComponentInChildren<RegularPolygon>();
        spriteRenderer = gameObject.GetComponentInChildren<SpriteRenderer>();
        body.transform.gameObject.SetActive(false);
        poof = gameObject.transform.GetChild(3).GetComponent<SpriteRenderer>();
        isMoving = true;
        spawning = true;
        spriteRenderer.color = Color.clear;
    }
    public void SetPosition(Vector2Int pos){
        position = pos;
        gameObject.transform.localPosition = (Vector2)position + Vector2.one*0.5f;
    }
    public void Destroy(){
        GameObject.Destroy(gameObject);
    }
    public void Reset(){
        sittingDown = false;
        dead = false;
        inPit = false;
        position = spawnPos;
        gameObject.transform.localPosition = (Vector2)position + Vector2.one*0.5f;
    }
    public void UndoReset(){
        //this needs to only move the amount of steps it took the last loop
        position = Services.Grid.playerStartPosition;
        for(int i = 0; i < Services.GameController.currentTurn;i++){
            if(moves.Count <= i){break;}
            position+=Services.Grid.directions[moves[i]];
        }
        gameObject.transform.localPosition = (Vector2)position;
    }
    //doesn't actually push, just checks to see if it can be pushed
    public bool CanPush(int direction, int depth){
        if(Services.Grid.tiles[position].canMove[direction] == false){
            if(Services.Grid.tiles[position].spikes[direction]){
                if(depth == 0){
                    //you're dead!
                    return false;
                }else{
                    //go through!
                    if(Services.Grid.tiles.ContainsKey(position+Services.Grid.directions[direction]) == false){
                        //you're dead!
                        return false;
                    }
                }
            }else{
                return false;
            }
        }
        Vector2Int newPos = position+Services.Grid.directions[direction];
        foreach(Player p in Services.GameController.players){
            if(p == this){continue;}
            if(p.position == newPos){
                return p.CanPush(direction, depth+1);
            }
        }
        return true;
    }
    public void Push(int direction){
        foreach(Player p in Services.GameController.players){
            if(p == this){continue;}
            if(p.position == position+Services.Grid.directions[direction]){
                p.BePushed(this,direction);
            }
        }
    }
    public void BePushed(Player pushert,int direction){
        pusher = pushert;
        Move(direction);
        foreach(Player p in Services.GameController.players){
            if(p == this){continue;}
            if(p.position == position){
                p.BePushed(this,direction);
            }
        }
    }
    public void Move(int direction){
        
        if(direction >= Services.Grid.directions.Length){return;}
        if(Services.GameController.state == GameState.LevelSelect){
            position+=Services.Grid.directions[direction];
            isMoving = true;
            if(sittingDown){
                changingSitting = true;
                animIndex = 0;
            }
            return;
        }
        //physically move, seperate from remembering it
        position+=Services.Grid.directions[direction];
        if(Services.Grid.tiles[position].CanWalk == false){
            Services.Grid.tiles[position].FillPit();
            inPit = true;
            dead = true;
            //TODO: figure out if an action point is already here!!
            foreach(ActionPoint ap in Services.Grid.actionPoints.Values){
                if(ap.held && ap.playerHolding == this){
                    ap.UnGrab();
                    break;
                }
            }
            position = new Vector2Int(-10,-10);
        }
        isMoving = true;
        if(sittingDown){
            changingSitting = true;
            animIndex = 0;
        }
    }
    public void FailureMove(int direction){
        failureDirection = direction;
        isMoving = true;
        if(sittingDown){
            changingSitting = true;
            animIndex = 0;
        }
    }
    public void AddMove(int direction){
        //add a move to your history
        moves.Add(direction);
    }
    public void PopMoves(){
        moves.RemoveAt(moves.Count-1);
    }
    public void Wait(){
        if(dead){
            return;
        }
        waitingStatus = 1;
        timeTurnStarted = Time.time;
        isMoving =true;
        if(sittingDown == false){
            Debug.Log("hoohaa");
            animIndex = 0;
            changingSitting = true;
        }
    }

    public void Draw(){
        if(spawning == false){
            spriteRenderer.color = Color.white;
        }
        //nose stuff
        int noseDirection = 2;
        int toCheck = Services.GameController.currentTurn;
        if(Services.GameController.currentPlayerIndex > index && Services.GameController.doClonesMove == false){
            toCheck++;
        }
        for(int i = toCheck; i>= 0; i--){
            if(i >= moves.Count){
                continue;
            }
            if(moves[i] <= 3){
                noseDirection = moves[i];
                break;
            }
        }
        if(Services.GameController.state == GameState.LevelSelect && moves.Count > 0){
            noseDirection = moves[moves.Count-1];
        }
        noseCenter.transform.localEulerAngles = new Vector3(0,0,Services.Visuals.angles[noseDirection]);
        //end nose stuff
        if(Services.GameController.currentLoop != index && moves.Count <= Services.GameController.currentTurn){
            spriteRenderer.color = new Color(1f,1f,1f,0.5f);
        }else{
            spriteRenderer.color = Color.white;
        }
        spriteRenderer.flipX = false;
        CharacterAnimationPack pack = Services.Visuals.playerPack;
        if(Services.GameController.currentLoop != index){
            pack = Services.Visuals.clone2Pack;
            if(index == Services.GameController.currentLoop-1){
                pack = Services.Visuals.clonePack;
            }
        }
        if(actuallyEating == false){
            if(isMoving){
                if(spawning){
                    animIndex+=Time.deltaTime*pack.idleAnimSpeed*2f;
                    int flooredIndex = Mathf.FloorToInt(animIndex);
                    if(flooredIndex >= pack.introAnimation.Count){
                        Debug.Log("aa");
                        animIndex = pack.introAnimation.Count-1;
                        spawning = false;
                        isMoving = false;
                        flooredIndex = Mathf.FloorToInt(animIndex);
                    }
                    spriteRenderer.color = Color.Lerp(new Color(1f,1f,1f,0f),Color.white,animIndex/pack.introAnimation.Count);
                    poof.sprite = pack.introAnimation[flooredIndex];
                    if(flooredIndex >= pack.idleAnimationDown.Count){
                        animIndex = 0;
                        flooredIndex = 0;
                    }
                    switch(noseDirection){
                        case 0:
                            spriteRenderer.sprite = pack.idleAnimationUp[flooredIndex];
                            break;
                        case 1:
                            spriteRenderer.sprite = pack.idleAnimationRight[flooredIndex];
                            break;
                        case 2:
                            spriteRenderer.sprite = pack.standingSpriteDown;
                            spriteRenderer.sprite = pack.idleAnimationDown[flooredIndex];
                            break;
                        case 3:
                            spriteRenderer.flipX = true;
                            spriteRenderer.sprite = pack.idleAnimationRight[flooredIndex];
                            break;
                    }
                }else{
                    if(changingSitting || (waitToMoveToUndoSitting && sittingDown)){
                        if(sittingDown){
                            animIndex+=Time.deltaTime*pack.standUpAnimSpeed;
                            int flooredIndex = Mathf.FloorToInt(animIndex);
                            if(flooredIndex >= pack.standUpAnimationDown.Count){
                                animIndex = pack.standUpAnimationDown.Count-1;
                                sittingDown = false;
                                changingSitting = false;
                                if(waitToMoveToUndoSitting){
                                    waitToMoveToUndoSitting = false;
                                }
                                flooredIndex = Mathf.FloorToInt(animIndex);
                            }
                            switch(noseDirection){
                                case 0:
                                    spriteRenderer.sprite = pack.standUpAnimationUp[flooredIndex];
                                    break;
                                case 1:
                                    spriteRenderer.sprite = pack.standUpAnimationRight[flooredIndex];
                                    break;
                                case 2:
                                    spriteRenderer.sprite = pack.standUpAnimationDown[flooredIndex];
                                    break;
                                case 3:
                                    spriteRenderer.flipX = true;
                                    spriteRenderer.sprite = pack.standUpAnimationRight[flooredIndex];
                                    break;
                            }
                        }else{
                            animIndex+=Time.deltaTime*pack.standUpAnimSpeed;
                            int flooredIndex = Mathf.FloorToInt(animIndex);
                            if(flooredIndex >= pack.sitDownAnimationDown.Count){
                                animIndex = pack.sitDownAnimationDown.Count-1;
                                isMoving = false;
                                sittingDown = true;
                                changingSitting = false;
                                flooredIndex = Mathf.FloorToInt(animIndex);
                            }
                            switch(noseDirection){
                                case 0:
                                    spriteRenderer.sprite = pack.sitDownAnimationUp[flooredIndex];
                                    break;
                                case 1:
                                    spriteRenderer.sprite = pack.sitDownAnimationRight[flooredIndex];
                                    break;
                                case 2:
                                    spriteRenderer.sprite = pack.sitDownAnimationDown[flooredIndex];
                                    break;
                                case 3:
                                    spriteRenderer.flipX = true;
                                    spriteRenderer.sprite = pack.sitDownAnimationRight[flooredIndex];
                                    break;
                            }
                            
                        }
                        
                    }else{
                        if(sittingDown){
                            switch(noseDirection){
                                case 0:
                                    spriteRenderer.sprite = pack.sitDownAnimationUp[pack.sitDownAnimationDown.Count-1];
                                    break;
                                case 1:
                                    spriteRenderer.sprite = pack.sitDownAnimationRight[pack.sitDownAnimationDown.Count-1];
                                    break;
                                case 2:
                                    spriteRenderer.sprite = pack.sitDownAnimationDown[pack.sitDownAnimationDown.Count-1];
                                    break;
                                case 3:
                                    spriteRenderer.flipX = true;
                                    spriteRenderer.sprite = pack.sitDownAnimationRight[pack.sitDownAnimationDown.Count-1];
                                    break;
                            }
                        }else{
                            animIndex+=Time.deltaTime*pack.walkAnimSpeed;
                            int flooredIndex = Mathf.FloorToInt(animIndex);
                            if(flooredIndex >= pack.walkAnimationRight.Count){
                                animIndex = 0;
                                flooredIndex = 0;
                            }
                            switch(noseDirection){
                                case 0:
                                    spriteRenderer.sprite = pack.walkAnimationUp[flooredIndex];
                                    break;
                                case 1:
                                    spriteRenderer.sprite = pack.walkAnimationRight[flooredIndex];
                                    break;
                                case 2:
                                    spriteRenderer.sprite = pack.walkAnimationDown[flooredIndex];
                                    break;
                                case 3:
                                    spriteRenderer.flipX = true;
                                    spriteRenderer.sprite = pack.walkAnimationRight[flooredIndex];
                                    break;
                            }
                        }
                        
                    }
                }
                
                
                
            }else{
                if(sittingDown){
                    switch(noseDirection){
                        case 0:
                            spriteRenderer.sprite = pack.sitDownAnimationUp[pack.sitDownAnimationDown.Count-1];
                            break;
                        case 1:
                            spriteRenderer.sprite = pack.sitDownAnimationRight[pack.sitDownAnimationDown.Count-1];
                            break;
                        case 2:
                            spriteRenderer.sprite = pack.sitDownAnimationDown[pack.sitDownAnimationDown.Count-1];
                            break;
                        case 3:
                            spriteRenderer.flipX = true;
                            spriteRenderer.sprite = pack.sitDownAnimationRight[pack.sitDownAnimationDown.Count-1];
                            break;
                    }
                }else{
                    animIndex+=Time.deltaTime*pack.idleAnimSpeed;
                    int flooredIndex = Mathf.FloorToInt(animIndex);
                    if(flooredIndex >= pack.idleAnimationDown.Count){
                        animIndex = 0;
                        flooredIndex = 0;
                    }
                    switch(noseDirection){
                        case 0:
                            spriteRenderer.sprite = pack.idleAnimationUp[flooredIndex];
                            break;
                        case 1:
                            spriteRenderer.sprite = pack.idleAnimationRight[flooredIndex];
                            break;
                        case 2:
                            spriteRenderer.sprite = pack.standingSpriteDown;
                            spriteRenderer.sprite = pack.idleAnimationDown[flooredIndex];
                            break;
                        case 3:
                            spriteRenderer.flipX = true;
                            spriteRenderer.sprite = pack.idleAnimationRight[flooredIndex];
                            break;
                    }
                }
                
                
            }
        }
        
        
        if(winning){
            gameObject.transform.eulerAngles += new Vector3(0,0,10f*(Time.deltaTime/0.016f));
            gameObject.transform.localScale += (Vector3.zero-gameObject.transform.localScale)*0.05f*(Time.deltaTime/0.016f);
        }else{
            gameObject.transform.eulerAngles = Vector3.zero;
            gameObject.transform.localScale = Vector3.one;
        }
        cloneNumber.text = (index+1).ToString();//((char)(index+65)).ToString();
        cloneNumber.color = Services.Visuals.tileColor;
        if(index == Services.GameController.currentLoop){
            cloneNumber.text = "<voffset=0.45em><sprite=6>";
            if(Services.GameController.canLoop){
                cloneNumber.text+="<line-height=80%><voffset=0.6em>\n<sprite=6>";
            }else if(dead){
                cloneNumber.text+="<line-height=70%>\nx";
            }
        }else{
            cloneNumber.text+="<line-height=70%>\n";
            int currentTurn = Services.GameController.currentTurn;
            if(Services.GameController.currentPlayerIndex > index && Services.GameController.doClonesMove == false){
                currentTurn++;
            }
            if(dead){
                cloneNumber.text+="x";
            }else{
                if(moves.Count-1 >= currentTurn){
                    cloneNumber.text+="<sprite="+moves[currentTurn].ToString()+">";
                }else{
                    //cloneNumber.text+="<sprite=4>";
                }
            }
            
        }
        if(Services.GameController.state == GameState.LevelSelect){
            cloneNumber.text = "";
        }
        body.Color = index != Services.GameController.currentLoop ? Services.Visuals.cloneColor : Services.Visuals.playerColor;
        head.Color = body.Color;
        legs[0].Color = head.Color;
        legs[1].Color = head.Color;
        body.SortingOrder = -index*5;
        head.SortingOrder = body.SortingOrder+2;
        legs[0].SortingOrder = body.SortingOrder;
        legs[1].SortingOrder = body.SortingOrder;
        cloneNumber.sortingOrder = body.SortingOrder+3;
        nose.SortingOrder = body.SortingOrder+1;
        if(isMoving == false){return;}
        if(changingSitting){
            return;
        }
        Debug.Log("A");
        if(ReferenceEquals(pusher,null) == false){
            if(Vector2.Distance(pusher.gameObject.transform.position,gameObject.transform.position) < 0.25f){
                pusher = null;
            }else{
                return;
            }
        }
        Vector3 targetPosition = (Vector3)(Vector2)position;
        targetPosition+=(Vector3)Vector2.one*0.5f;
        if(failureDirection != -1){
            targetPosition += (Vector3)(Vector2)Services.Grid.directions[failureDirection]*0.5f;
        }
        if(sittingDown == false){
            float t = (Mathf.Sin(Time.time*20f)+1f)/2f;
            legs[0].transform.localPosition =(new Vector3(-0.1f,Mathf.Lerp(-0.2f,-0.05f,t),0f));
            legs[1].transform.localPosition =(new Vector3(0.1f,Mathf.Lerp(-0.05f,-0.2f,t),0f));
        }
        
        if(Vector2.Distance(targetPosition,gameObject.transform.localPosition) < 0.1f){
            
            gameObject.transform.localPosition = targetPosition;
            
            if(eating){
                if(actuallyEating == false){
                    animIndex = 0;
                }
                actuallyEating = true;
                animIndex+=Time.deltaTime*pack.chompAnimSpeed;
                int flooredIndex = Mathf.FloorToInt(animIndex);
                Debug.Log(flooredIndex.ToString()+" : "+pack.chompAnimationRight.Count);
                if(flooredIndex > pack.chompAnimationRight.Count-1){
                    Debug.Log("DONE!");
                    animIndex = pack.chompAnimationRight.Count-1;
                    eating = false;
                    actuallyEating = false;
                    flooredIndex = Mathf.FloorToInt(animIndex);
                }
                switch(noseDirection){
                    case 0:
                        spriteRenderer.sprite = pack.chompAnimationUp[flooredIndex];
                        break;
                    case 1:
                        spriteRenderer.sprite = pack.chompAnimationRight[flooredIndex];
                        break;
                    case 2:
                        spriteRenderer.sprite = pack.chompAnimationDown[flooredIndex];
                        break;
                    case 3:
                        spriteRenderer.flipX = true;
                        spriteRenderer.sprite = pack.chompAnimationRight[flooredIndex];
                        break;
                }
                if(eating){
                    return;
                }
                
            }
            if(spawning){
                return;
            }
            isMoving = false;
            if(failureDirection != -1){
                failureDirection = -1;
                isMoving = true;
            }
            if(isMoving == false && sittingDown == false){
                legs[0].transform.localPosition =new Vector3(-0.1f,-0.2f,0f);
                legs[1].transform.localPosition =new Vector3(0.1f,-0.2f,0f);
            }
            if(waitToMoveToUndoSitting){
                waitToMoveToUndoSitting = false;
                changingSitting = true;
                isMoving = true;
            }
            
        }
        
        gameObject.transform.localPosition += (targetPosition-gameObject.transform.localPosition).normalized*Services.Visuals.lerpSpeed*Services.Visuals.actualWalkSpeed;
    }
    public void FollowMouse(Vector2 pos){
        Vector3 target = (Vector3)(pos);
        target.x += 0.1f*Mathf.Sin(Time.time*5f);
        target.y += 0.1f*Mathf.Cos(Time.time*3f);
        gameObject.transform.position = target;
    }
}
public class PlayerState{
    public Vector2Int position;
    public Vector2Int spawnPos;
    public List<int> moves;
    public bool sittingDown;
    public bool dead;
    public PlayerState(Player player){
        position = player.position;
        spawnPos = player.spawnPos;
        sittingDown = player.sittingDown;
        dead = player.dead;
        moves = new List<int>();
        foreach(int i in player.moves){
            moves.Add(i);
        }
    }
}