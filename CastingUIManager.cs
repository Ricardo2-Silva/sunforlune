using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CastingUIManager : MonoBehaviour
{
    //Instância Singleton
    public static CastingUIManager Instance { get; private set; }

    [Header("UI Referências")]
    public GameObject castBarPanel;
    public Text abilityNameText;
    public Text castTimeText;
    public Image castFillImage;

    private Coroutine currentRoutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional, if you want it to persist
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void ShowCastBar(string abilityName, float duration)
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        castBarPanel.SetActive(true);
        abilityNameText.text = abilityName;
        currentRoutine = StartCoroutine(FillBar(duration));
    }

    public void HideCastBar()
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);
        castFillImage.fillAmount = 0f;
        castTimeText.text = "";
        castBarPanel.SetActive(false);

    }

    //Atualiza manualmente o preenchimento da barra (usado em WaterGun.cs)
    public void UpdateFill(float percent, float castTime)
    {
        castFillImage.fillAmount = percent;
        float remainingTime = castTime * (1f - percent);
        castTimeText.text = remainingTime.ToString("F1");
    }

    private IEnumerator FillBar(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            castFillImage.fillAmount = elapsed / duration;
            elapsed += Time.deltaTime;
            yield return null;
        }
        castFillImage.fillAmount = 1f;
    }
}
