using UnityEngine;

public sealed class SimpleProjectilePrefabSetup : MonoBehaviour
{
    public float Radius = 0.2f;

    private void Reset()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        if (sr.sprite == null)
        {
            Texture2D t = new Texture2D(16, 16);
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                    t.SetPixel(x, y, Color.white);
            t.Apply();
            Rect r = new Rect(0, 0, 16, 16);
            sr.sprite = Sprite.Create(t, r, new Vector2(0.5f, 0.5f), 16f / (Radius * 2f));
        }
        transform.localScale = Vector3.one * (Radius * 2f);
    }
}
