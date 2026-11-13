using UnityEngine;
using System.Collections;

public class EnemyHitFlash : MonoBehaviour
{
    [SerializeField] private Material flashMaterial;
    [SerializeField] private float flashDuration = 0.08f;

    private Renderer[] renderers;
    private Material[][] originalMaterials;
    private Coroutine flashRoutine;

    private void Awake()
    {
        //gets all renderers/mesh renderers
        renderers = GetComponentsInChildren<Renderer>();
        originalMaterials = new Material[renderers.Length][];

        //storing the renderer references to put them back on later
        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].sharedMaterials;
        }
    }

    //called in the enemy scripts
    public void TriggerFlash()
    {
        if (flashMaterial == null || renderers.Length == 0) return;

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        //swaps every material on the enemy to the flash material
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] flashArray = new Material[originalMaterials[i].Length];
            for (int j = 0; j < flashArray.Length; j++)
            {
                flashArray[j] = flashMaterial;
            }
            renderers[i].sharedMaterials = flashArray;
        }

        
        yield return new WaitForSecondsRealtime(flashDuration);

        //restoring materials back
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].sharedMaterials = originalMaterials[i];
            }
        }

        flashRoutine = null;
    }
}