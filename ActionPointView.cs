using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum ActionPointStates { InLevel, WithPlayer, InUI }

public class ActionPointView : MonoBehaviour
{
    public ActionPointStates state;
    public GameObject orbSprite;
    // Bounce Animation Section
    public float bounceDistance = 1;
    public float bounceRate = 0.5f;
    private Coroutine bounceRoutine;

    public void InitializeView(ActionPointStates currentState)
    {
        state = currentState;

        if (state == ActionPointStates.InLevel)
        {
            bounceRoutine = StartCoroutine(OrbBounceRoutine());
        }
    }

    /// <summary>
    /// Makes the orb bounce up and down
    /// </summary>
    /// <returns></returns>
    public IEnumerator OrbBounceRoutine()
    {
        float timer = 0;
        bool goingUp = true;
        Vector3 orgPos = orbSprite.transform.localPosition;
        Vector3 newPos = orgPos + Vector3.up * bounceDistance;
        while (true)
        {
            if (timer > bounceRate)
            {
                timer = 0;
                goingUp = !goingUp;
            }

            if (goingUp)
            {
                orbSprite.transform.localPosition = Vector3.Lerp(orgPos, newPos, (timer / bounceRate));
            }
            else
            {
                orbSprite.transform.localPosition = Vector3.Lerp(newPos, orgPos, (timer / bounceRate));
            }
            timer += Time.deltaTime;
        }
    }
}
