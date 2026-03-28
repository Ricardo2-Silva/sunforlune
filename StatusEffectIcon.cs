using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatusEffectIcon : MonoBehaviour
{
    public Image iconImage;
    public TextMeshProUGUI durationText;
    public TextMeshProUGUI stacksText;

    public void SetIcon(Sprite sprite)
    {
        if (iconImage != null)
            iconImage.sprite = sprite;
    }

    public void SetDuration(float seconds)
    {
        if (durationText != null)
            durationText.text = seconds > 0 ? Mathf.CeilToInt(seconds).ToString() + "s" : "";
    }

    public void SetStacks(int stacks)
    {
        if (stacksText != null)
            stacksText.text = stacks > 1 ? stacks.ToString() + "x" : "";
    }
}
