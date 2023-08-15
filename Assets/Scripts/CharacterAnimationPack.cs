using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterAnimationPack")]
public class CharacterAnimationPack : ScriptableObject
{
    public List<Sprite> walkAnimationRight;
    public List<Sprite> walkAnimationUp;
    public List<Sprite> walkAnimationDown;
    public float walkAnimSpeed = 5f;
    public Sprite standingSpriteRight;
    public Sprite standingSpriteUp;
    public Sprite standingSpriteDown;
    public List<Sprite> idleAnimationDown;
    public float idleAnimSpeed = 3.5f;
    public List<Sprite> sitDownAnimationDown;
    public List<Sprite> standUpAnimationDown;
    public float standUpAnimSpeed = 20f;
    public List<Sprite> chompAnimation;
    public float chompAnimSpeed = 5f;
}
