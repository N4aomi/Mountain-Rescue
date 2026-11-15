using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class RescueManager : MonoBehaviour
{
    [Header("Player")]
    public Transform player;
    public PlayerHealth playerHealth;
    public GameHUD hud;

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
                "RescueManager: References missing!"
            );

            enabled = false;
            return;
        }

        if (playerHealth == null)
            playerHealth = player.GetComponent<PlayerHealth>();

        List<Transform> availableSpots =
            new List<Transform>(hidingPlaces);

        // randomize hiding missions
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
                Debug.LogError("A hiker or hiding is missing!");
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

        ShowMessage("Rescue mission started: Find all 3 hikers!");
    }

    private void Update()
    {
        if (missionFinished || Time.timeScale == 0f ||
            (playerHealth != null && playerHealth.IsDead))
        {
            if (hud != null) hud.SetInteractionPrompt(false);
            return;
        }

        UpdateInteractionPrompt();

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

    private HikerAI FindNearestWaitingHiker()
    {
        HikerAI nearest = null;
        float closest = interactDistance;

        foreach (HikerAI hiker in hikers)
        {
            if (hiker == null || hiker.CurrentState != HikerAI.HikerState.Waiting)
                continue;

            float distance = Vector3.Distance(player.position, hiker.transform.position);
            if (distance <= closest)
            {
                closest = distance;
                nearest = hiker;
            }
        }

        return nearest;
    }

    private void UpdateInteractionPrompt()
    {
        if (hud == null) return;
        bool show = !evacuationStarted && activeHiker == null &&
                    FindNearestWaitingHiker() != null;
        hud.SetInteractionPrompt(show);
    }

    private void ShowMessage(string message)
    {
        if (hud != null) hud.ShowMessage(message);
        else Debug.Log(message);
    }

    private void CheckForInteraction()
    {
        // Only one hiker at a time
        if (activeHiker != null)
            return;

        if (Keyboard.current == null ||
            !Keyboard.current.eKey.wasPressedThisFrame)
            return;

        HikerAI nearestHiker = FindNearestWaitingHiker();

        if (nearestHiker != null)
        {
            activeHiker = nearestHiker;
            activeHiker.StartFollowing();

            ShowMessage(activeHiker.name + " is following you. Return to camp!");
            if (hud != null) hud.SetInteractionPrompt(false);
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

        // Player and Hikers have to be at camp
        if (playerDistance <= campRadius &&
            hikerDistance <= campRadius)
        {
            activeHiker.ArriveAtCamp();
            activeHiker = null;

            rescuedHikers++;

            ShowMessage("Hiker rescued! " + rescuedHikers + "/3");

            if (rescuedHikers == hikers.Length)
            {
                BeginEvacuation();
            }
        }
    }

    private void BeginEvacuation()
    {
        evacuationStarted = true;

        ShowMessage("All hikers rescued! Go to the cave exit!");

        foreach (HikerAI hiker in hikers)
        {
            hiker.StartEvacuation();
        }
    }

    private void CheckCaveExit()
    {
        // Player has to be at exit
        if (Vector3.Distance(
            player.position,
            caveExitPoint.position) > exitRadius)
            return;

        // All Hikers have to be there
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

        ShowMessage("MISSION COMPLETE! Everyone is safe!");

        Time.timeScale = 1f;
        SceneManager.LoadScene("EndScene");
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
