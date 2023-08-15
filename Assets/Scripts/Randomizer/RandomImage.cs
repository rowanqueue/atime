using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomImage : MonoBehaviour
{
    SpriteRenderer sr;
    public bool neverDestroy = false;
    public List<Sprite> sprites;
    // Start is called before the first frame update
    void Start()
    {
        sr = gameObject.GetComponent<SpriteRenderer>();
        /*if(Random.value < 0 && neverDestroy == false){
            Destroy(gameObject);
            
            return;
        }*/
        sr.sprite = sprites[Random.Range(0,sprites.Count)];
        Destroy(this);
    }
}
