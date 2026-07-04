using UnityEngine;
using System.Collections.Generic;

public static class MobileInput
{
    public static bool LeftHeld;
    public static bool RightHeld;
    public static bool UpHeld;
    public static bool DownHeld;

    public static bool ZHeld;
    public static bool XHeld;
    public static bool CHeld;
    public static bool SHeld;
    public static bool QHeld;
    public static bool EHeld;
    public static bool PauseHeld;

    private static HashSet<string> clickedButtons = new HashSet<string>();

    public static void RegisterClick(string buttonName)
    {
        clickedButtons.Add(buttonName);
    }

    public static bool GetKeyDown(string buttonName)
    {
        if (clickedButtons.Contains(buttonName))
        {
            clickedButtons.Remove(buttonName);
            return true;
        }
        return false;
    }

    public static void ClearFrameClicks()
    {
        clickedButtons.Clear();
    }
}