using UnityEngine;
/* PixelExplosion.cs: efecto visual animado de explosi�n usando sprites por frame. */
using System.Collections;
using System.Collections.Generic;

public class PixelExplosion : MonoBehaviour
{
    private SpriteRenderer sr;
    private List<Sprite> frames = new List<Sprite>();
    public float frameRate = 18f; // Fotogramas por segundo

    // Cache estatica de los fotogramas para no recargarlos en cada explosion.
    private static Sprite[] cachedFrames;

    private void Awake()
    {
        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 25; // Draw on top of everything

        LoadFrames();
    }

    private void LoadFrames()
    {
        // 1) Cargar desde Resources: funciona TANTO en el Editor como en la build.
        if (cachedFrames == null)
        {
            Sprite[] loaded = Resources.LoadAll<Sprite>("VFX/Explosion 2 SpriteSheet");

#if UNITY_EDITOR
            // 2) Fallback solo-editor si por algun motivo no estan en Resources.
            if (loaded == null || loaded.Length == 0)
            {
                Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/SpriteSheets/layers/Explosion 2 SpriteSheet.png");
                List<Sprite> list = new List<Sprite>();
                foreach (var asset in assets)
                    if (asset is Sprite sprite) list.Add(sprite);
                loaded = list.ToArray();
            }
#endif

            // Ordenar los fotogramas alfabeticamente para reproducirlos en el orden correcto.
            SortedDictionary<string, Sprite> sortedFrames = new SortedDictionary<string, Sprite>();
            if (loaded != null)
            {
                foreach (var s in loaded)
                    if (s != null) sortedFrames[s.name] = s;
            }
            List<Sprite> ordered = new List<Sprite>();
            foreach (var kvp in sortedFrames) ordered.Add(kvp.Value);
            cachedFrames = ordered.ToArray();
        }

        frames.Clear();
        frames.AddRange(cachedFrames);
    }

    private void Start()
    {
        if (transform.localScale == Vector3.one)
        {
            transform.localScale = new Vector3(2.5f, 2.5f, 1f);
        }

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

    public static void CreateExplosion(Vector3 position, float scale)
    {
        GameObject go = new GameObject("PixelExplosionEffect");
        go.transform.position = position;
        go.transform.localScale = new Vector3(scale, scale, 1f);
        go.AddComponent<PixelExplosion>();
    }
}
