using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;

//we take the level data string and make it this class to further process it
public class Level
{
    public string internal_name;
    public string name;
    public int index;
    public Vector2Int gridPosition;
    public GameObject gameObject;
    public bool on = false;
    public int turnLimit;
    public Vector2Int startPosition;
    public Vector2Int extent;
    //things
    public Dictionary<Vector2Int,Tile> tiles = new Dictionary<Vector2Int, Tile>();
    public Dictionary<Vector2Int,ActionPoint> actionPoints = new Dictionary<Vector2Int, ActionPoint>();
    public List<ActionPoint> turnPoints = new List<ActionPoint>(); //these are turns you start with
    public List<Player> players = new List<Player>();
    public Dictionary<Vector2Int,Exit> exits = new Dictionary<Vector2Int, Exit>();
    public GameObject startMark;
    public Dictionary<Vector2Int,TreeTile> treeTiles = new Dictionary<Vector2Int, TreeTile>();
    //end things
    public string levelData;
    public bool changed;//this was changed in the level editor and its still opened and hasnt been saved
    public bool beingCarried;

    //LEVEL SELECT BORDERS
    public bool careAboutBorders => section >= 0;
    public int section;//0 is no section, 1-n is section index
    public int sectionExit;//-1 if pos does not have one lol
    public string spoken = "";
    public string timer_key = "abcdef";
    public Level(LevelJson levelJson){
        //this is created only for the new level select
    }
    public Level(string levelData,string _internal_name){
        gameObject = new GameObject();
        gameObject.transform.parent = Services.Grid.transform;
        
        this.levelData = levelData;
        string[] lines = levelData.Split('\n');
        name = lines[0].Trim();
        internal_name = _internal_name;
        gameObject.name = name;
        string[] posInfo = lines[1].Split(',');
        gridPosition = new Vector2Int(int.Parse(posInfo[0]),int.Parse(posInfo[1]));
        section = -1;
        if(posInfo.Length >= 3){
            section = int.Parse(posInfo[2]);
        }
        sectionExit = -1;
        if(posInfo.Length >= 4){
            sectionExit = int.Parse(posInfo[3]);
        }
        gameObject.transform.localPosition = (Vector2)gridPosition;
        string[] moreInfo = lines[2].Split(',');
        if(moreInfo.Length > 1){
            spoken = moreInfo[1];
        }
        turnLimit = int.Parse(moreInfo[0]);
        for(var i = 0; i < turnLimit;i++){
            turnPoints.Add(new ActionPoint(i,gameObject.transform));
        }
        extent = Vector2Int.zero;
        for(int y = 3; y < lines.Length;y++){
            string[] line = lines[y].Split(',');
            for(int x = 0; x < line.Length;x++){
                if(line[x].Contains("_")){
                    continue;
                }
                Vector2Int pos = new Vector2Int(x,lines.Length-y-1);
                Tile newTile = MakeTile(pos);
                tiles.Add(pos,newTile);
                if(line[x].Contains("@")){
                    startPosition = pos;
                    players.Add(new Player(pos,gameObject.transform));
                }
                if(line[x].Contains("*")){
                    actionPoints.Add(pos,MakeActionPoint(pos));
                    if(line[x][0] != '*'){
                        actionPoints[pos].num = int.Parse(line[x][0].ToString());
                    }
                    for(int i = 0; i < timer_key.Length;i++){
                        if(line[x].Contains(timer_key[i].ToString())){
                            actionPoints[pos].timer = i+1;
                        }
                    }
                }
                if(line[x].Contains("x")){
                    exits.Add(pos,new Exit(pos,gameObject.transform));
                    exits[pos].level = this;
                }
                if(line[x].Contains("u")){
                    AddSpawnPortal(pos);
                }
                if(line[x].Contains("s")){
                    AddSpores(pos);
                }
                //0-3 are walls
                //4-7 are spikes
                for(var i = 0; i < 4;i++){
                    if(line[x][0].ToString() == i.ToString()){
                        continue;
                    }
                    if(line[x].Contains(i.ToString())){
                        newTile.walls[i] = true;
                    }
                    if(line[x].Contains((i+4).ToString())){
                        newTile.spikes[i] = true;
                    }
                }
            }
        }
        startMark = GameObject.Instantiate(Services.GameController.startMarkPrefab,gameObject.transform);
        startMark.transform.localPosition = (Vector2)startPosition;
        FindExtents();
        FindNeighborsBetweenTiles();

        //make the treetiles
        foreach(Vector2Int pos in tiles.Keys){
            for(int i = 0; i < Services.Grid.directions.Length;i++){
                Vector2Int new_pos = pos+Services.Grid.directions[i];
                if(tiles.ContainsKey(new_pos)){
                    continue;
                }
                if(treeTiles.ContainsKey(new_pos)){
                    continue;
                }
                AddTreeTile(new_pos);
            }
        }
        for(var j = 0; j < 3;j++){
            List<Vector2Int> _trees = treeTiles.Keys.ToList();
            foreach(Vector2Int pos in _trees){
                for(int i = 0; i < Services.Grid.directions.Length;i++){
                    Vector2Int new_pos = pos+Services.Grid.directions[i];
                    if(tiles.ContainsKey(new_pos)){
                        continue;
                    }
                    if(treeTiles.ContainsKey(new_pos)){
                        continue;
                    }
                    AddTreeTile(new_pos,j+1);
                }
            }
        }
        
    }
    //like the borders/lines between levels in leve select
    public void ThinkAboutBorders(){
        //if(careAboutBorders == false){return;}
        bool[] borderNeeded = new bool[4];
        for(var i = 0; i < 4; i++){
            borderNeeded[i] = true;
            if(Services.LevelSelect.v2Level.ContainsKey(gridPosition+Services.Grid.directions[i])){
                if(Services.LevelSelect.v2Level[gridPosition+Services.Grid.directions[i]].section == section || sectionExit == i){
                    borderNeeded[i] = false;
                }
                
            }
        }
        for(var i = 0; i < 4; i++){
            if(borderNeeded[i] == false){continue;}
            LineRenderer newBorder = GameObject.Instantiate(Services.GameController.linePrefab,Services.LevelSelect.borderParent).GetComponent<LineRenderer>();
            newBorder.transform.position = (Vector2)gridPosition;
            newBorder.positionCount = 2;
            newBorder.SetPosition(0,(Vector2)Services.Grid.corners[(i-1+4)%4]);
            newBorder.SetPosition(1,(Vector2)Services.Grid.corners[i]);
            newBorder.startWidth/=2f;
            newBorder.endWidth=newBorder.startWidth;
            Services.LevelSelect.sections[section].edges.Add(newBorder);
        }

    }
    void FindNeighborsBetweenTiles(){
        foreach(Tile tile in tiles.Values){
            for(int i = 0; i < Services.Grid.directions.Length;i++){
                tile.canMove[i] = true;
                if(tile.walls[i]){tile.canMove[i] = false;}
                if(tiles.ContainsKey(tile.position+Services.Grid.directions[i])){
                    tile.neighbors[i] = tiles[tile.position+Services.Grid.directions[i]];
                    if(tiles[tile.position+Services.Grid.directions[i]].walls[Services.Grid.opposite(i)] == false){
                        tile.canMove[i] = true;
                    }else{
                        tile.canMove[i] = false;
                    }
                }else{
                    tile.canMove[i] = false;
                }
            }
        }
    }
    void FindExtents(){
        Vector2Int lowest = Vector2Int.zero;
        Vector2Int highest = Vector2Int.zero;
        foreach(Vector2Int pos in tiles.Keys){
            if(pos.x < lowest.x){lowest.x = pos.x;}
            if(pos.x > highest.x){highest.x = pos.x;}
            if(pos.y < lowest.y){lowest.y = pos.y;}
            if(pos.y > highest.y){highest.y = pos.y;}
        }
        extent = new Vector2Int(1+highest.x-lowest.x,1+highest.y-lowest.y);
    }
    Tile MakeTile(Vector2Int position){
        Tile tile = new Tile(position,gameObject.transform);
        return tile;
    }
    ActionPoint MakeActionPoint(Vector2Int pos){
        ActionPoint ap = new ActionPoint(pos,gameObject.transform);
        return ap;
    }
    public void Reset(){
        //reset tiles
        //reset action points
        for(int i = Services.GameController.turnLimitDisplay.Count-1; i>=0;i--){
            ActionPoint ap = Services.GameController.turnLimitDisplay[i];
            if(ap.startedAsTurn == false){
                ap.UndoCollect();
            }else{
                ap.gameObject.transform.parent = Services.Grid.level.gameObject.transform;
                Services.GameController.turnLimitDisplay.RemoveAt(i);
            }
        }
        foreach(ActionPoint ap in actionPoints.Values){
            ap.held = false;
            ap.on = true;
        }
        //reset players
        for(int i = 0; i < players.Count;i++){
            players[i].Destroy();
        }
        players.Clear();
        players.Add(new Player(startPosition,gameObject.transform));
    }
    public void InstantShrink(){
        float shrink = 0.25f;
        bool perfect = extent.x == extent.y;
        bool xBigger = false;
        if(extent.x > extent.y){
            shrink = 1f/((float)extent.x+1f);
            xBigger = true;
        }else{
            shrink = 1f/((float)extent.y+1f);
        }
        gameObject.transform.localScale = Vector3.one*shrink;
        Vector2 targetPosition = (Vector2)gridPosition;
        targetPosition+= Vector2.one*shrink*0.5f;
        if(perfect == false){
            if(!xBigger){
                targetPosition += Vector2.right*shrink*0.5f*(extent.y-extent.x);
            }else{
                targetPosition += Vector2.up*shrink*0.5f*(extent.x-extent.y);
            }
        }
        
        
        gameObject.transform.position = targetPosition;
        foreach(ActionPoint turnPoint in turnPoints){
            turnPoint.Hide();
        }
        foreach(ActionPoint actionPoint in actionPoints.Values){
            actionPoint.InstantShrink();
        }
        foreach(Exit exit in exits.Values){
            exit.InstantShrink();
        }
        
    }
    public void InstantCenter(){
        Vector2 targetPosition = new Vector3(Camera.main.transform.position.x,Camera.main.transform.position.y);
            targetPosition.x-=(extent.x*0.5f);
            targetPosition.y-=(extent.y*0.5f);
            gameObject.transform.position = targetPosition;
            gameObject.transform.localScale = Vector3.one;
    }
    public void DrawLevel(){
        if(on){
            gameObject.transform.localScale = Services.Visuals.LerpVector(gameObject.transform.localScale,Vector3.one);
            Vector2 targetPosition = new Vector3(Camera.main.transform.position.x,Camera.main.transform.position.y);
            targetPosition.x-=(extent.x*0.5f);
            targetPosition.y-=(extent.y*0.5f);
            gameObject.transform.position = Services.Visuals.LerpVector(gameObject.transform.position,targetPosition);
            foreach(ActionPoint turnPoint in turnPoints){
                turnPoint.Show();
            }
        }else{
            if(Services.GameController.state == GameState.LevelSelect){
                gameObject.SetActive(Services.LevelSelect.unlocked[index]);
            }else{
                gameObject.SetActive(false);
            }
            
            float shrink = 0.25f;
            bool perfect = extent.x == extent.y;
            bool xBigger = false;
            if(extent.x > extent.y){
                shrink = 1f/((float)extent.x+1f);
                xBigger = true;
            }else{
                shrink = 1f/((float)extent.y+1f);
            }
            gameObject.transform.localScale = Services.Visuals.LerpVector(gameObject.transform.localScale,Vector3.one*shrink);
            Vector2 targetPosition = (Vector2)gridPosition;
            targetPosition+= Vector2.one*shrink*0.5f;
            if(perfect == false){
                if(!xBigger){
                    targetPosition += Vector2.right*shrink*0.5f*(extent.y-extent.x);
                }else{
                    targetPosition += Vector2.up*shrink*0.5f*(extent.x-extent.y);
                }
            }
            if(beingCarried){
                Vector2 target = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition)-Vector2.one*0.5f;
                target.x += 0.1f*Mathf.Sin(Time.time*5f);
                target.y += 0.1f*Mathf.Cos(Time.time*3f);
                
                gameObject.transform.position = target;
            }else{
                gameObject.transform.position = Services.Visuals.LerpVector(gameObject.transform.position,targetPosition);
            }
            
            foreach(ActionPoint turnPoint in turnPoints){
                turnPoint.Hide();
            }
        }
        foreach(Tile tile in tiles.Values){
            tile.Draw(Services.LevelSelect.won[index]);
        }
        foreach(ActionPoint actionPoint in actionPoints.Values){
            actionPoint.Draw();
        }
        
        foreach(Player player in players){
            player.Draw();
        }
        foreach(Exit exit in exits.Values){
            exit.Draw();
        }
    }
    //LEVEL EDITING SHIT
    #if UNITY_EDITOR
    void ReworkOrigin(Vector2Int change){
        List<Tile> tempTiles = new List<Tile>();
        foreach(Tile tile in tiles.Values){
            tempTiles.Add(tile);
            tile.SetPosition(tile.position+change);
        }
        tiles.Clear();
        foreach(Tile tile in tempTiles){
            tiles.Add(tile.position,tile);
        }
        List<ActionPoint> tempPoints = new List<ActionPoint>();
        foreach(ActionPoint ap in actionPoints.Values){
            tempPoints.Add(ap);
            ap.gridPosition+=change;
            ap.gameObject.transform.localPosition =(Vector2)ap.gridPosition;
        }
        actionPoints.Clear();
        foreach(ActionPoint ap in tempPoints){
            actionPoints.Add(ap.gridPosition,ap);
        }
        List<Exit> tempExits = new List<Exit>();
        foreach(Exit exit in exits.Values){
            tempExits.Add(exit);
            exit.SetPosition(exit.position+change);
        }
        exits.Clear();
        foreach(Exit exit in tempExits){
            exits.Add(exit.position,exit);
        }
        players[0].SetPosition(players[0].position+change);
        startPosition+=change;
        startMark.transform.localPosition = (Vector2)startPosition;
    }
    public void AddTreeTile(Vector2Int pos,int depth = 0){
        if(treeTiles.ContainsKey(pos)){
            if(treeTiles[pos].hideTree == false){
                treeTiles[pos].HideTree(true);
            }
            return;
        }
        treeTiles.Add(pos,new TreeTile(pos,gameObject.transform,this,depth));
    }
    public void RemoveTreeTile(Vector2Int pos){
        treeTiles[pos].Destroy();
        treeTiles.Remove(pos);
    }
    public void AddTile(Vector2Int pos){
        changed = true;
        Vector2Int originChange = Vector2Int.zero;
        if(pos.x < 0){
            int add = Mathf.Abs(pos.x-0);
            originChange.x = add;
            pos.x+=add;
        }
        if(pos.y < 0){
            int add = Mathf.Abs(pos.y-0);
            originChange.y = add;
            pos.y+=add;
        }
        if(originChange != Vector2Int.zero){
            ReworkOrigin(originChange);
        }
        tiles.Add(pos,new Tile(pos,gameObject.transform));
        FindExtents();
        FindNeighborsBetweenTiles();
    }
    public void RemoveTile(Vector2Int pos){
        changed = true;
        Tile t = tiles[pos];
        for(int i = 0; i < t.neighbors.Length;i++){
            if(t.walls[i]){
                t.neighbors[i].walls[Services.Grid.opposite(i)] = false;
            }
        }
        tiles[pos].Destroy();
        tiles.Remove(pos);
        if(pos.x == 0 || pos.y == 0){
            bool anotherInThisColumn = false;//same x
            bool anotherInThisRow = false;//same y
            Vector2Int originChange = new Vector2Int(100,100);
            foreach(Tile tile in tiles.Values){
                if(tile.position == pos){continue;}
                if(tile.position.x == pos.x){
                    anotherInThisColumn = true;
                }
                if(Mathf.Abs(tile.position.x-pos.x) < originChange.x){
                    originChange.x = Mathf.Abs(tile.position.x-pos.x);
                }
                if(tile.position.y == pos.y){
                    anotherInThisRow = true;
                }
                if(Mathf.Abs(tile.position.y-pos.y) < originChange.y){
                    originChange.y = Mathf.Abs(tile.position.y-pos.y);
                }
            }
            if(anotherInThisColumn || pos.x != 0){
                originChange.x = 0;
            }
            if(anotherInThisRow || pos.y != 0){
                originChange.y = 0;
            }
            /*if(pos.x == 0){
                originChange.x*=-1;
            }
            if(pos.y == 0){
                originChange.x*=-1;
            }*/
            if(originChange != Vector2Int.zero){
                ReworkOrigin(originChange*-1);
            }
        }
        FindExtents();
        FindNeighborsBetweenTiles();
    }
    public void AddActionPoint(Vector2Int pos){
        changed = true;
        actionPoints.Add(pos,new ActionPoint(pos,gameObject.transform));
        actionPoints[pos].gameObject.transform.localPosition = (Vector2)actionPoints[pos].gridPosition;
    }
    public void RemoveActionPoint(Vector2Int pos){
        changed = true;
        if(actionPoints[pos].num > 1){
            actionPoints[pos].num-=1;
        }else{
            actionPoints[pos].Destroy();
            actionPoints.Remove(pos);
        }
    }
    public void RemoveActionPointTimer(Vector2Int pos){
        changed = true;
        if(actionPoints[pos].timer > 0){
            actionPoints[pos].timer-=1;
        }
    }
    public void AddToActionPoint(Vector2Int pos){
        changed = true;
        if(actionPoints[pos].num < 9){
            actionPoints[pos].num+=1;
        }
    }
    public void AddToActionPointTimer(Vector2Int pos){
        changed = true;
        if(actionPoints[pos].timer < 6){
            actionPoints[pos].timer+=1;
        }
        
    }
    public void AddPit(Vector2Int pos){
        changed = true;
        tiles[pos].AddPit();
    }
    public void RemovePit(Vector2Int pos){
        changed = true;
        tiles[pos].RemovePit();
    }
    public void AddSpores(Vector2Int pos){
        if(tiles[pos].hasSpores){
            return;
        }
        changed = true;
        tiles[pos].AddSpores();
    }
    public void RemoveSpores(Vector2Int pos){
        changed = true;
        tiles[pos].RemoveSpores();
    }
    public void AddSpawnPortal(Vector2Int pos){
        if(tiles[pos].hasSpawnPortal){
            return;
        }
        changed = true;
        tiles[pos].AddSpawnPortal();
    }
    public void RemoveSpawnPortal(Vector2Int pos){
        changed = true;
        tiles[pos].RemoveSpawnPortal();
    }
    public void MoveExit(Exit exit, Vector2Int pos){
        changed = true;
        exits.Remove(exit.position);
        exits.Add(pos,exit);
        exit.SetPosition(pos);
    }
    public void AddExit(Exit exit){
        changed = true;
        exits.Add(exit.position,exit);
        exit.level = this;
    }
    public void RemoveExit(Vector2Int pos){
        changed = true;
        exits[pos].Destroy();
        exits.Remove(pos);
    }
    public void MoveStart(Vector2Int pos){
        changed = true;
        startPosition = pos;
        players[0].SetPosition(pos);
        startMark.transform.localPosition = (Vector2)startPosition;
    }
    public void RaiseTurnLimit(){
        turnLimit++;
        changed = true;
        Services.GameController.turnLimit++;
        turnPoints.Add(new ActionPoint(turnLimit-1,gameObject.transform));
        turnPoints[turnPoints.Count-1].gameObject.transform.parent = Services.GameController.turnLimitParent;
        turnPoints[turnPoints.Count-1].gameObject.transform.localScale = Vector3.one;
        Services.GameController.turnLimitDisplay.Add(turnPoints[turnPoints.Count-1]);
    }
    public void LowerTurnLimit(){
        turnLimit--;
        Services.GameController.turnLimit--;
        Services.GameController.turnLimitDisplay.RemoveAt(turnPoints.Count-1);
        changed = true;
        turnPoints[turnPoints.Count-1].Destroy();
        turnPoints.RemoveAt(turnPoints.Count-1);
    }
    public void AddWall(Vector2Int pos, int direction){
        //gotta check if you're allowed
        Tile tile = tiles[pos];
        if(tile.walls[direction]){return;}//wall here already
        if(tile.canMove[direction] == false){return;}//no neighbor
        changed = true;
        tile.walls[direction] = true;
        tile.canMove[direction] = false;
        tile.neighbors[direction].walls[Services.Grid.opposite(direction)] = true;
        tile.neighbors[direction].canMove[Services.Grid.opposite(direction)] = false;
    }
    public void AddWallSpike(Vector2Int pos, int direction){
        Tile tile = tiles[pos];
        if(tile.neighbors[direction] == null){return;}
        changed = true;
        tile.walls[direction] = true;
        tile.spikes[direction] = true;
        tile.canMove[direction] = false;
        tile.neighbors[direction].walls[Services.Grid.opposite(direction)] = true;
        tile.neighbors[direction].spikes[Services.Grid.opposite(direction)] = true;
        tile.neighbors[direction].canMove[Services.Grid.opposite(direction)] = false;
    }
    public void RemoveWall(Vector2Int pos, int direction){
        Tile tile = tiles[pos];
        if(tile.walls[direction] == false){return;}
        if(tile.neighbors[direction] == null){return;}
        changed = true;
        tile.walls[direction] = false;
        tile.canMove[direction] = true;
        tile.neighbors[direction].walls[Services.Grid.opposite(direction)] = false;
        tile.neighbors[direction].canMove[Services.Grid.opposite(direction)] = true;
    }
    public void RemoveWallSpike(Vector2Int pos, int direction){
        Tile tile = tiles[pos];
        if(tile.spikes[direction] == false){return;}
        if(tile.neighbors[direction] == null){return;}
        changed = true;
        tile.spikes[direction] = false;
        tile.neighbors[direction].spikes[Services.Grid.opposite(direction)] = false;
    }
    public void ConvertLevelToJson(){
        LevelJson json = new LevelJson();
        Debug.Log(internal_name);
        Debug.Log(name);
        json.internal_name = internal_name;
        json.name = name;
        json.map_pos = new LevelJson.Position(gridPosition.x,gridPosition.y);
        json.map_section = section;
        json.section_exit = sectionExit;

        json.turn_limit = turnLimit;
        json.text = spoken;

        json.start = new LevelJson.Position(startPosition.x,startPosition.y);

        json.tiles = new List<LevelJson.Tile>();
        json.walls = new List<LevelJson.Wall>();
        foreach(Vector2Int _pos in tiles.Keys){
            LevelJson.Tile _tile = new LevelJson.Tile();
            _tile.x = _pos.x;
            _tile.y = _pos.y;
            if(actionPoints.ContainsKey(_pos)){
                _tile.thing = "a";
            }
            if(exits.ContainsKey(_pos)){
                _tile.thing = "x";
            }
            json.tiles.Add(_tile);

            //walls
            for(var i = 0; i < 4; i++){
                if(tiles[_pos].walls[i] == false){
                    continue;
                }
                LevelJson.Wall _wall = new LevelJson.Wall();
                _wall.x = _pos.x;
                _wall.y = _pos.y;
                if(i == 1){
                    _wall.x+=1;
                }
                if(i == 2){
                    _wall.y-=1;
                }
                switch(i){
                    case 0:
                        _wall.is_up = true;
                        break;
                    case 1:
                        _wall.is_up = false;
                        break;
                    case 2:
                        _wall.is_up = true;
                        break;
                    case 3:
                        _wall.is_up = false;
                        break;
                }
                bool contains = false;
                foreach(LevelJson.Wall oWall in json.walls){
                    if(oWall == _wall){
                        contains = true;
                        break;
                    }
                }
                if(contains == false){
                    json.walls.Add(_wall);
                }
                
            }
            
        }

        

        string json_string = JsonUtility.ToJson(json);
        string path = Application.dataPath+"/_Levels/"+internal_name+".json";
        if(!File.Exists(path)){
            System.IO.File.WriteAllText(path,json_string);
        }else{
            StreamWriter writer = new StreamWriter(path, false);
            writer.NewLine = "";
            writer.WriteLine(json_string);
            writer.Close();
        }

    }
    public string ConvertLevelToString(){
        string s = "";
        string[,] tileInfo = new string[extent.x,extent.y];
        foreach(ActionPoint ap in actionPoints.Values){
            string _ap = "*";
            if(ap.num > 1){
                _ap=ap.num.ToString()+_ap;
            }
            if(ap.timer > 0){
                _ap+=timer_key[ap.timer-1];
            }
            tileInfo[ap.gridPosition.x,ap.gridPosition.y] = _ap;
            
        }
        tileInfo[startPosition.x,startPosition.y] = "@";
        foreach(Exit exit in exits.Values){
            tileInfo[exit.position.x,exit.position.y] = "x";
        }
        foreach(Tile tile in tiles.Values){
            string t = ".";
            if(tile.hasPit){
                t = "u";
            }
            if(tile.hasSpores){
                t = "s";
            }
            if(tileInfo[tile.position.x,tile.position.y] != null){
                t = tileInfo[tile.position.x,tile.position.y];
            }
            
            for(int i = 0; i < tile.walls.Length;i++){
                if(tile.walls[i]){
                    t+=i.ToString();
                }
            }
            for(int i = 0; i < tile.spikes.Length;i++){
                if(tile.spikes[i]){
                    t+=(i+4).ToString();
                }
            }
            tileInfo[tile.position.x,tile.position.y] = t;
        }
        
        for(var y = extent.y-1; y >= 0; y--){
            if(y!=extent.y-1){
                s+='\n';
            }
            for(var x = 0; x < extent.x;x++){
                if(x!=0){
                    s+=",";
                }
                if(tileInfo[x,y] == null){
                    s+="_";
                }else{
                    s+=tileInfo[x,y];
                }
            }
        }
        Debug.Log(s);
        return s;
    }
    public void Destroy(){
        GameObject.Destroy(gameObject);
    }
    #endif
}
[System.Serializable]
public class LevelJson{
    public string internal_name;
    public string name;
    [System.Serializable]
    public class Position{
        public int x;
        public int y;
        public Position(int _x, int _y){
            this.x = _x;
            this.y = _y;
        }
    }
    public Position map_pos;
    public int map_section;
    public int section_exit;
    public int turn_limit;
    public string text;
    public Position start;
    [System.Serializable]
    public class Tile{
        public int x;
        public int y;
        public string thing;
    }
    public List<Tile> tiles;
    [System.Serializable]
    public class Wall{
        public int x;
        public int y;
        public bool is_up;
        public int type;
        public static bool operator ==(Wall a, Wall b)
        { 
            if(a.x == b.x && a.y == b.y && a.is_up == b.is_up){
                return true;
            }else{
                return false;
            }
        }
        public static bool operator !=(Wall a, Wall b)
        { 
            if(a.x == b.x && a.y == b.y && a.is_up == b.is_up){
                return false;
            }else{
                return true;
            }
        }
    }
    public List<Wall> walls;

}