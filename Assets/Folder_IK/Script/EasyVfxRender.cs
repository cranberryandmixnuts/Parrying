using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

public class VFXBlink : MonoBehaviour
{
    private VFXRenderer vfxRenderer;

    void Start()
    {
        vfxRenderer = GetComponent<VFXRenderer>();
        StartCoroutine(BlinkRoutine());
    }

    IEnumerator BlinkRoutine()
    {
        while (true)
        {
            vfxRenderer.enabled = !vfxRenderer.enabled;
            yield return new WaitForSeconds(1f);
        }
    }
}