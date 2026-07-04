using UnityEngine;
using UnityEngine.EventSystems;

public class TouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public string buttonName; // e.g. "Left", "Right", "Up", "Down", "Z", "X", "C", "S", "Q", "Pause"

    public void OnPointerDown(PointerEventData eventData)
    {
        SetButtonState(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        SetButtonState(false);
    }

    private void SetButtonState(bool pressed)
    {
        switch (buttonName)
        {
            case "Left": MobileInput.LeftHeld = pressed; break;
            case "Right": MobileInput.RightHeld = pressed; break;
            case "Up": MobileInput.UpHeld = pressed; break;
            case "Down": MobileInput.DownHeld = pressed; break;
            case "Z": MobileInput.ZHeld = pressed; if (pressed) MobileInput.RegisterClick("Z"); break;
            case "X": MobileInput.XHeld = pressed; if (pressed) MobileInput.RegisterClick("X"); break;
            case "C": MobileInput.CHeld = pressed; if (pressed) MobileInput.RegisterClick("C"); break;
            case "S": MobileInput.SHeld = pressed; if (pressed) MobileInput.RegisterClick("S"); break;
            case "Q": MobileInput.QHeld = pressed; if (pressed) MobileInput.RegisterClick("Q"); break;
            case "Pause": MobileInput.PauseHeld = pressed; if (pressed) MobileInput.RegisterClick("Pause"); break;
        }
    }

    private void OnDisable()
    {
        // Reset state on disable to prevent getting stuck
        SetButtonState(false);
    }
}