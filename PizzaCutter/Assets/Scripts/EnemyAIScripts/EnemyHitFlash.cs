using UnityEngine;
using System.Collections;

public class EnemyHitFlash : MonoBehaviour
{
    [SerializeField] private Material flashMaterial;
    [SerializeField] private float flashDuration = 0.08f;

   
    [SerializeField] private float dissolveDuration = 2f;

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
        RestoreOriginalMaterials();
        flashRoutine = null;
    }


    private void RestoreOriginalMaterials()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].sharedMaterials = originalMaterials[i];
            }
        }
    }



    //DEATH EFFECT
    //call in other scripts
    public void TriggerDeathDissolve()
    {
        Debug.Log("TriggerDeathDissolve was called on: " + gameObject.name);


        if (renderers == null || renderers.Length == 0)
        {
            Debug.LogError("No renderers found on " + gameObject.name + "!");
            return;
        }


        RestoreOriginalMaterials();
        //unparenting so destroying the enemy object doesnt also destroy this
        transform.SetParent(null);
        StartCoroutine(DissolveAndDestroyRoutine());
    }

    private IEnumerator DissolveAndDestroyRoutine()
    {
        float elapsed = 0f;

        Material[][] activeMaterials = new Material[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
        {
            activeMaterials[i] = renderers[i].materials;

            foreach (var mat in activeMaterials[i])
            {
                SetupMaterialForTransparent(mat);
            }

        }

        while (elapsed < dissolveDuration)
        {
            float t = elapsed / dissolveDuration;

            //Shifts color then drops alpha
            Color targetColor = Color.Lerp(Color.red, new Color(0.5f, 0f, 0f, 0f), t);

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    for (int j = 0; j < activeMaterials[i].Length; j++)
                    {
                        if (activeMaterials[i][j].HasProperty("_BaseColor"))
                        {
                            activeMaterials[i][j].SetColor("_BaseColor", targetColor);
                        }
                        else if (activeMaterials[i][j].HasProperty("_Color"))
                        {
                            activeMaterials[i][j].SetColor("_Color", targetColor);
                        }
                    }
                }
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        //destroys self
        Destroy(gameObject);
    }


    private void SetupMaterialForTransparent(Material material)
    {
        //this is so much easier in unreal aaaaaaaaaaa
        material.SetFloat("_Surface", 1.0f);
        material.SetFloat("_Blend", 0.0f);  
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);      
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }



}