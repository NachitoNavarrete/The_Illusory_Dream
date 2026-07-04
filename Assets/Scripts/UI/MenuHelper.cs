using UnityEngine;

public class MenuHelper : MonoBehaviour
{
    public GameObject helpPanel;

    public void ShowHelp()
    {
        if (helpPanel != null)
        {
            helpPanel.SetActive(true);
        }
    }

    public void HideHelp()
    {
        if (helpPanel != null)
        {
            helpPanel.SetActive(false);
        }
    }
}
