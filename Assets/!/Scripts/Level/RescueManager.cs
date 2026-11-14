using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class RescueManager : MonoBehaviour
{
    [Header("Player")]
    public Transform player;

    [Header("Hikers - Exactly 3")]
    public HikerAI[] hikers = new HikerAI[3];

    [Header("Random Hiding Places")]
    public Transform[] hidingPlaces;

    [Header("Mission Locations")]
    public Transform campPoint;
    public Transform caveExitPoint;

    [Header("Distances")]
    public float interactDistance = 3f;
    public float campRadius = 5f;
    public float exitRadius = 7f;

    [Header("End Scene")]
    public string endSceneName = "Endscene";

    [Header("Mission Progress")]
    [SerializeField] private int rescuedHikers = 0;
    [SerializeField] private bool evacuationStarted = false;

    private HikerAI activeHiker;
    private bool missionFinished;

    private void Start()
    {
        if (player == null ||
            campPoint == null ||
            caveExitPoint == null ||
            hikers.Length != 3 ||
            hidingPlaces.Length < 3)
        {
            Debug.LogError(
                "RescueManager: References fehlen!"
            );

            enabled = false;
            return;
        }

        List<Transform> availableSpots =
            new List<Transform>(hidingPlaces);

        // Verstecke zufällig mischen
        for (int i = availableSpots.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);

            Transform temp = availableSpots[i];
            availableSpots[i] = availableSpots[randomIndex];
            availableSpots[randomIndex] = temp;
        }

        for (int i = 0; i < hikers.Length; i++)
        {
            if (hikers[i] == null ||
                availableSpots[i] == null)
            {
                Debug.LogError("Ein Wanderer oder Versteck fehlt!");
                enabled = false;
                return;
            }

            bool success = hikers[i].Initialize(
                player,
                availableSpots[i].position,
                i
            );

            if (!success)
            {
                enabled = false;
                return;
            }
        }

        Debug.Log("Rettungsmission gestartet: 0/3");
    }

    private void Update()
    {
        if (missionFinished)
            return;

        if (!evacuationStarted)
        {
            CheckForInteraction();
            CheckCampArrival();
        }
        else
        {
            CheckCaveExit();
        }
    }

    private void CheckForInteraction()
    {
        // Nur einen Wanderer gleichzeitig mitnehmen
        if (activeHiker != null)
            return;

        if (Keyboard.current == null ||
            !Keyboard.current.eKey.wasPressedThisFrame)
            return;

        HikerAI nearestHiker = null;
        float nearestDistance = interactDistance;

        foreach (HikerAI hiker in hikers)
        {
            if (hiker.CurrentState !=
                HikerAI.HikerState.Waiting)
                continue;

            float distance = Vector3.Distance(
                player.position,
                hiker.transform.position
            );

            if (distance <= nearestDistance)
            {
                nearestDistance = distance;
                nearestHiker = hiker;
            }
        }

        if (nearestHiker != null)
        {
            activeHiker = nearestHiker;
            activeHiker.StartFollowing();

            Debug.Log(
                activeHiker.name +
                " folgt dir jetzt! Bring ihn zum Camp."
            );
        }
    }

    private void CheckCampArrival()
    {
        if (activeHiker == null)
            return;

        float playerDistance = Vector3.Distance(
            player.position,
            campPoint.position
        );

        float hikerDistance = Vector3.Distance(
            activeHiker.transform.position,
            campPoint.position
        );

        // Player UND Wanderer müssen im Camp sein
        if (playerDistance <= campRadius &&
            hikerDistance <= campRadius)
        {
            activeHiker.ArriveAtCamp();
            activeHiker = null;

            rescuedHikers++;

            Debug.Log(
                "Wanderer gerettet! " +
                rescuedHikers + "/3"
            );

            if (rescuedHikers == hikers.Length)
            {
                BeginEvacuation();
            }
        }
    }

    private void BeginEvacuation()
    {
        evacuationStarted = true;

        Debug.Log(
            "Alle Wanderer gerettet! " +
            "Bringe sie zum Höhlenausgang!"
        );

        foreach (HikerAI hiker in hikers)
        {
            hiker.StartEvacuation();
        }
    }

    private void CheckCaveExit()
    {
        // Der Spieler muss am Ausgang sein
        if (Vector3.Distance(
            player.position,
            caveExitPoint.position) > exitRadius)
            return;

        // Alle drei Wanderer müssen ebenfalls da sein
        foreach (HikerAI hiker in hikers)
        {
            if (Vector3.Distance(
                hiker.transform.position,
                caveExitPoint.position) > exitRadius)
            {
                return;
            }
        }

        FinishMission();
    }

    private void FinishMission()
    {
        missionFinished = true;

        Debug.Log("MISSION COMPLETE! Alle gerettet!");

        Time.timeScale = 1f;
        SceneManager.LoadScene(endSceneName);
    }

    private void OnDrawGizmosSelected()
    {
        if (campPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(
                campPoint.position,
                campRadius
            );
        }

        if (caveExitPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(
                caveExitPoint.position,
                exitRadius
            );
        }
    }
}
