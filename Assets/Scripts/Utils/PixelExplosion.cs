using UnityEngine;
/* PixelExplosion.cs: efecto visual animado de explosión usando sprites por frame. */
using System.Collections;
using System.Collections.Generic;

public class PixelExplosion : MonoBehaviour
{
    private SpriteRenderer sr;
    private List<Sprite> frames = new List<Sprite>();
    public float frameRate = 18f; // Fotogramas por segundo

    private void Awake()
    {
        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 25; // Draw on top of everything
        transform.localScale = new Vector3(2.5f, 2.5f, 1f); // Make it beautifully scaled up

#if UNITY_EDITOR
        // Cargar sprites directamente desde la hoja de sprites del proyecto
        string path = "Assets/SpriteSheets/layers/Explosion 2 SpriteSheet.png";
        Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
        
        // Ordenar los fotogramas alfabéticamente para reproducirlos en el orden correcto
        SortedDictionary<string, Sprite> sortedFrames = new SortedDictionary<string, Sprite>();
        foreach (var asset in assets)
        {
            if (asset is Sprite sprite)
            {
                sortedFrames[sprite.name] = sprite;
            }
        }

        foreach (var kvp in sortedFrames)
        {
            frames.Add(kvp.Value);
        }
#endif
    }

    private void Start()
    {
        if (frames.Count > 0)
        {
            StartCoroutine(PlayAnimationRoutine());
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator PlayAnimationRoutine()
    {
        float delay = 1f / frameRate;
        for (int i = 0; i < frames.Count; i++)
        {
            if (sr != null)
            {
                sr.sprite = frames[i];
            }
            yield return new WaitForSeconds(delay);
        }
        Destroy(gameObject);
    }

    public static void CreateExplosion(Vector3 position)
    {
        GameObject go = new GameObject("PixelExplosionEffect");
        go.transform.position = position;
        go.AddComponent<PixelExplosion>();
    }
}
