using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeTile
{
    public Vector2Int position;
    public bool hideTree;
    public int depth = 0;
    GameObject gameObject;
    SpriteRenderer tree;
    SpriteRenderer other;

    public TreeTile(Vector2Int position, Transform parent,Level level, int _depth = 0){
        this.depth = _depth;
        this.position = position;
        gameObject = GameObject.Instantiate(Services.GameController.treeTilePrefab,(Vector2)position,Quaternion.identity,parent);
        gameObject.transform.localPosition = (Vector2)position;
        tree = gameObject.transform.GetChild(0).GetChild(0).GetComponent<SpriteRenderer>();
        other = gameObject.transform.GetChild(0).GetChild(1).GetComponent<SpriteRenderer>();
        tree.sortingOrder = -this.position.y;
        other.sortingOrder = tree.sortingOrder-1;
        if(Random.value < 0.5f){
            tree.flipX = true;
        }
        if(Random.value < 0.5f){
            other.flipX = true;
        }
        if(Random.value < 0.9f){
            other.enabled = false;
        }
        RandomPlacement random = tree.gameObject.AddComponent<RandomPlacement>();
        random.lowest = new Vector2(-0.25f,-0.25f);
        random.highest = new Vector2(0.25f,0.25f);
        //north/south stuff
        //check for stuff north of you
        bool moreBushChance = false;
        if(level.tiles.ContainsKey(position+Vector2Int.up) || level.tiles.ContainsKey(position+Vector2Int.up*2)){
            HideTree(true);
            moreBushChance = true;
            
        }else if(level.tiles.ContainsKey(position+Vector2Int.up*3)){
            random.highest.y = 0f;
            random.lowest.y = -0.25f;
        }
        //check for stuff south of you
        if(level.tiles.ContainsKey(position+Vector2Int.down)){
            random.highest.y = -0.1f;
            random.lowest.y = -0.5f;
            moreBushChance = true;
        }
        //is there a tile immedaitely to the right of you?
        if(level.tiles.ContainsKey(position+Vector2Int.right) 
            || level.tiles.ContainsKey(position+Vector2Int.right+Vector2Int.up)
            || level.tiles.ContainsKey(position+Vector2Int.right+Vector2Int.up*2)){
            random.highest.x = 0f;
        }
         if(level.tiles.ContainsKey(position+Vector2Int.left) 
            || level.tiles.ContainsKey(position+Vector2Int.left+Vector2Int.up)
            || level.tiles.ContainsKey(position+Vector2Int.left+Vector2Int.up*2)){
            random.lowest.x = 0f;
        }
        if(Random.value < (moreBushChance ? 0.4f : 0.2f)){
            GameObject bush = GameObject.Instantiate(Services.GameController.bushPrefab,gameObject.transform);
            bush.GetComponent<SpriteRenderer>().sortingOrder = -position.y;
        }
    }
    public void Destroy(){
        GameObject.Destroy(gameObject);
    }
    public void HideTree(bool yes){
        hideTree = yes;
        tree.enabled = !yes;
    }
}
