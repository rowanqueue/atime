using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomImage : MonoBehaviour
{
    public SpriteRenderer sr;
    public List<Sprite> sprites;
    // Start is called before the first frame update
    void Start()
    {
        if(Random.value < 0.6){
            Destroy(gameObject);
            
            return;
        }
        sr.sprite = sprites[Random.Range(0,sprites.Count)];
        Destroy(this);
    }
}
