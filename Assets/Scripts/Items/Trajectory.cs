using Sortify;
using System;
using UnityEngine;

public class Trajectory : MonoBehaviour
{
    [BetterHeader("References")]
    [SerializeField] private GameObject dotPrefab;
    [SerializeField] private DragAndShoot dragAndShoot;

    [BetterHeader("Settings")]
    [Range(3, 50)]
    [SerializeField] private int dotsNumber;
    [RangeStep(0.01f, 1f, 0.01f)]
    [SerializeField] private float dotSpacing;

    private Vector3 dotPos;
    private float timeStamp; // Position of dots along the trajectory
    private Transform[] dotsList;
    private GameObject dotsParent;
    private Vector3 adjustedForce;
    private Vector3 adjustedForceDamping;
    private float time; // current time of the dots

    public void Initialize(Transform dotsParentTransform)
    {
        dotsParent = dotsParentTransform.gameObject;
        PrepareDots();
        Hide();
    }

    private void PrepareDots()
    {
        dotsList = new Transform[dotsNumber];
        for (int i = 0; i < dotsNumber; i++)
        {
            dotsList[i] = Instantiate(dotPrefab, dotsParent.transform).transform; // Create dots based on the number of dots variable
            dotsList[i].position = dotsParent.transform.position; // set the dots position to the parent position (in player)
        }
    }

    public void UpdateDots(Vector3 objectPos, Vector3 forceApplied, ItemSO itemSO) // add the object mass to calculate the trajectory, we put 10 as default because in item prefabs we change for 10
    {
        timeStamp = dotSpacing;
        adjustedForce = forceApplied / itemSO.mass; // Adjust the force to the weight of the object
        adjustedForceDamping = forceApplied / itemSO.linearDamping; // Adjust the force to the linear damping of the object


        if (itemSO.linearDamping > 0)
        {
            for (int i = 0; i < dotsNumber; i++)
            {
                time = timeStamp; // we update the time for each dot

                float expDecay = Mathf.Exp(-itemSO.linearDamping * time);

                dotPos.x = objectPos.x + adjustedForce.x * time; // Formula to calculate the position of the dots along the trajectory
                dotPos.y = objectPos.y + adjustedForce.y * time * expDecay + (0.5f * Physics.gravity.y * time * time);
                dotPos.z = objectPos.z + adjustedForce.z * time; // we have to maintain the z position, for the dots to be in the same plane as the player

                dotsList[i].position = dotPos;
                timeStamp += dotSpacing; // increase the time stamp to move the dots further along the trajectory
                Debug.Log("Adjusted Force " + adjustedForce + "dot.pos.y " + dotPos.y + "ExpDecay " + expDecay + "Time.Stamp " + timeStamp);
            }
        }
        else
        {
            for (int i = 0; i < dotsNumber; i++)
            {
                time = timeStamp; // we update the time for each dot

                dotPos.x = objectPos.x + adjustedForce.x * time; // Formula to calculate the position of the dots along the trajectory
                dotPos.y = objectPos.y + adjustedForce.y * time + (0.5f * Physics.gravity.y * time * time);
                dotPos.z = objectPos.z + adjustedForce.z * time; // we have to maintain the z position, for the dots to be in the same plane as the player

                dotsList[i].position = dotPos;
                timeStamp += dotSpacing; // increase the time stamp to move the dots further along the trajectory
                Debug.Log("errado");
            }
        }  
    }

    public void Show()
    {
        dotsParent.SetActive(true);
    }

    public void Hide()
    {
        dotsParent.SetActive(false);
    }
}
