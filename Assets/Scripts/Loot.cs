/*
/* Loot: define un ítem que un enemigo puede soltar (itemPrefab) y su probabilidad (dropChance). */
using UnityEngine;

[System.Serializable]
public class Loot
{
    public GameObject itemPrefab;
    [Range(0, 100)] public float dropChance;
}
