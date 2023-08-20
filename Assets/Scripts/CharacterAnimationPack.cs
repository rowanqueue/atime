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
    public List<Sprite> idleAnimationRight;
    public List<Sprite> idleAnimationUp;
    public float idleAnimSpeed = 3.5f;
    public List<Sprite> sitDownAnimationDown;
    public List<Sprite> sitDownAnimationRight;
    public List<Sprite> sitDownAnimationUp;
    public List<Sprite> standUpAnimationDown;
    public List<Sprite> standUpAnimationRight;
    public List<Sprite> standUpAnimationUp;
    public float standUpAnimSpeed = 20f;
    public List<Sprite> chompAnimation;
    public float chompAnimSpeed = 5f;
}
