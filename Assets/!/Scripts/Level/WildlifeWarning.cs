using System.Collections;
using TMPro;
using UnityEngine;

public class WildlifeWarning : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text warningText;

    [Header("Settings")]
    public float warningDuration = 3f;

    private Coroutine warningCoroutine;

    private void Start()
    {
        if (warningText != null)
        {
            warningText.gameObject.SetActive(false);
        }
    }

    public void ShowWarning()
    {
        if (warningText == null)
            return;

        if (warningCoroutine != null)
        {
            StopCoroutine(warningCoroutine);
        }

        warningCoroutine = StartCoroutine(DisplayWarning());
    }

    private IEnumerator DisplayWarning()
    {
        warningText.text = "Beware of Wildlife!";
        warningText.gameObject.SetActive(true);

        yield return new WaitForSecondsRealtime(warningDuration);

        warningText.gameObject.SetActive(false);
        warningCoroutine = null;
    }
}
