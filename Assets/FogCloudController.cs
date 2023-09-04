using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FogCloudController : MonoBehaviour
{
    [Header("Parameters")]
    [SerializeField] private SpriteRenderer fogSprite;
    [SerializeField] private float speedMin;
    [SerializeField] private float speedMax;
    [Header("Info")]
    [SerializeField] private int depth;
    [SerializeField] private float screenStart;
    [SerializeField] private float screenEnd;
    [SerializeField] private float speed;
    // TODO: Randomize size
    // TODO: Randomize height

    public void Initialize(int depth, float screenStartXPos, float screenEndXPos)
    {
        this.depth = depth;
        screenStart = screenStartXPos;
        screenEnd = screenEndXPos;
        fogSprite.sortingOrder = depth;
        speed = Random.Range(speedMin/1000, speedMax/1000);
    }

    private void Update()
    {
        transform.position += Vector3.right * speed;
        if (transform.position.x > screenEnd + fogSprite.sprite.bounds.extents.x)
        {
            transform.position = new Vector3(screenStart - fogSprite.sprite.bounds.extents.x, transform.position.y, transform.position.z);
        }
    }
}
