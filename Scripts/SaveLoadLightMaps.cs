using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SaveLoadLightMaps : MonoBehaviour
{
    public string folderName { get { return m_folderName; } }

    [SerializeField] private string m_folderName = "lightmap_1";
    private string m_jsonFileName = "lighting.json";

    private AllLightMapData m_allLightMapData;

    [System.Serializable]
    private class SphericalHarmonics
    {
        public float[] coefficients = new float[27];
    }

    [System.Serializable]
    private class RendererInfo
    {
        public string id;
        public int lightmapIndex;
        public Vector4 lightmapScaleOffset;
    }

    [System.Serializable]
    private class AllLightMapData
    {
        public RendererInfo[] rendererInfos;
        public string[] lightmapsColorNames;
        public string[] lightmapsDirNames;
        public LightmapsMode lightmapsMode;
        public SphericalHarmonics[] lightProbes;
    }

    public void LoadLightmaps(string folderName)
    {
        m_folderName = folderName;
        LoadLightmaps();
    }

    public void LoadLightmaps()
    {
        string filePath = Application.dataPath + "/Resources/LightMaps/" + SceneManager.GetActiveScene().name + "/" + m_folderName + "/";

        if (!Directory.Exists(filePath))
        {
            Debug.Log(filePath + " not found");
            return;
        }

        //Read json file
        string json = "";
        if(File.Exists(filePath + m_jsonFileName))
        {
            json = File.ReadAllText(filePath + m_jsonFileName);
        }
        m_allLightMapData = JsonUtility.FromJson<AllLightMapData>(json);

        //Load lightmap textures from resources directory
        LightmapData[] loadedLightMaps = new LightmapData[m_allLightMapData.lightmapsColorNames.Length];
        for(int i = 0; i < loadedLightMaps.Length; i++)
        {
            loadedLightMaps[i] = new LightmapData();
            loadedLightMaps[i].lightmapColor = Resources.Load<Texture2D>("LightMaps/" + SceneManager.GetActiveScene().name + "/" + m_folderName + "/" + m_allLightMapData.lightmapsColorNames[i]);

            if(m_allLightMapData.lightmapsMode != LightmapsMode.NonDirectional)
            {
                loadedLightMaps[i].lightmapDir = Resources.Load<Texture2D>("LightMaps/" + SceneManager.GetActiveScene().name + "/" + m_folderName + "/" + m_allLightMapData.lightmapsDirNames[i]);
            }
        }

        //Calculate light probe data (idk what this is doing tbh this is literally the one part of this that i didn't rewrite lol)
        SphericalHarmonicsL2[] sphericalHarmonicsArray = new SphericalHarmonicsL2[m_allLightMapData.lightProbes.Length];
        for (int i = 0; i < m_allLightMapData.lightProbes.Length; i++)
        {
            SphericalHarmonicsL2 sphericalHarmonics = new SphericalHarmonicsL2();
            // j is coefficient
            for (int j = 0; j < 3; j++)
            {
                //k is channel ( r g b )
                for (int k = 0; k < 9; k++)
                {
                    sphericalHarmonics[j, k] = m_allLightMapData.lightProbes[i].coefficients[j * 9 + k];
                }
            }
            sphericalHarmonicsArray[i] = sphericalHarmonics;
        }
        LightmapSettings.lightProbes.bakedProbes = sphericalHarmonicsArray;

        //Get all objects with a uniqueID
        UniqueId[] allRenderers = FindObjectsOfType<UniqueId>();

        //Scale light map textures from loaded file
        for (int i = 0; i < m_allLightMapData.rendererInfos.Length; i++)
        {
            RendererInfo info = m_allLightMapData.rendererInfos[i];

            //Look for object with the same ID as the renderer read from json
            for(int j = 0; j < allRenderers.Length; j++)
            {
                if (info.id == allRenderers[j].GetID())
                {
                    //Grab mesh renderer reference
                    MeshRenderer renderer = allRenderers[j].GetComponent<MeshRenderer>();

                    //Set renderer properties
                    renderer.lightmapIndex = m_allLightMapData.rendererInfos[i].lightmapIndex;
                    if (!renderer.isPartOfStaticBatch)
                    {
                        renderer.lightmapScaleOffset = m_allLightMapData.rendererInfos[i].lightmapScaleOffset;
                    }
                    if (renderer.isPartOfStaticBatch)
                    {
                        Debug.Log("Object " + renderer.gameObject.name + " is part of static batch, skipping lightmap offset and scale.");
                    }

                    //IDs are unique, so break after one is found
                    break;
                }
            }
        }

        LightmapSettings.lightmaps = loadedLightMaps;
        Debug.Log("Lightmaps loaded");
    }

#if UNITY_EDITOR

    public void SaveLightmaps()
    {
        m_allLightMapData = new AllLightMapData();
        List<RendererInfo> allRenderersInfo = new List<RendererInfo>();
        Texture2D[] allLightmapsColor = new Texture2D[LightmapSettings.lightmaps.Length];
        Texture2D[] allLightmapsDir = new Texture2D[LightmapSettings.lightmaps.Length];
        List<SphericalHarmonics> allLightProbeCoeffs = new List<SphericalHarmonics>();

        //Get every mesh renderer in the scene
        MeshRenderer[] renderers = (MeshRenderer[])FindObjectsOfType(typeof(MeshRenderer));
        foreach(MeshRenderer renderer in renderers)
        {
            //Get data from every mesh renderer that uses a lightmap
            if(renderer.lightmapIndex != -1)
            {
                //Generate and save unique ID for renderer if one doesn't exist
                UniqueId id;
                if (!renderer.TryGetComponent<UniqueId>(out id))
                {
                    id = renderer.gameObject.AddComponent<UniqueId>();
                    id.GenerateID();
                }

                //Store info about the current mesh renderer
                RendererInfo currentInfo = new RendererInfo();
                currentInfo.id = id.GetID();
                currentInfo.lightmapIndex = renderer.lightmapIndex;
                currentInfo.lightmapScaleOffset = renderer.lightmapScaleOffset;

                //Store all color lightmaps at the same index used by the mesh renderer
                allLightmapsColor[renderer.lightmapIndex] = LightmapSettings.lightmaps[renderer.lightmapIndex].lightmapColor;

                //Store all directional lightmaps at the same index used by the mesh renderer
                if(LightmapSettings.lightmapsMode != LightmapsMode.NonDirectional)
                {
                    allLightmapsDir[renderer.lightmapIndex] = LightmapSettings.lightmaps[renderer.lightmapIndex].lightmapDir;
                }

                //Add current mesh renderer's info to collective list of mesh renderer info
                allRenderersInfo.Add(currentInfo);
            }
        }

        m_allLightMapData.lightmapsMode = LightmapSettings.lightmapsMode;
        m_allLightMapData.rendererInfos = allRenderersInfo.ToArray();

        //Store light probe data
        SphericalHarmonicsL2[] lightProbes = LightmapSettings.lightProbes.bakedProbes;
        for (int i = 0; i < lightProbes.Length; i++)
        {
            SphericalHarmonics lightProbeCoeff = new SphericalHarmonics();
            // j is coefficient
            for (int j = 0; j < 3; j++)
            {
                //k is channel ( r g b )
                for (int k = 0; k < 9; k++)
                {
                    lightProbeCoeff.coefficients[j * 9 + k] = lightProbes[i][j, k];
                }
            }
            allLightProbeCoeffs.Add(lightProbeCoeff);
        }

        m_allLightMapData.lightProbes = allLightProbeCoeffs.ToArray();

        //Copy light maps and renderer data to resource directory for the scene
        string filePath = Application.dataPath + "/Resources/LightMaps/" + SceneManager.GetActiveScene().name + "/" + m_folderName + "/";
        if (!Directory.Exists(filePath))
        {
            Directory.CreateDirectory(filePath);
        }
        m_allLightMapData.lightmapsColorNames = CopyTextures(filePath, allLightmapsColor);
        m_allLightMapData.lightmapsDirNames = CopyTextures(filePath, allLightmapsDir);

        string json = JsonUtility.ToJson(m_allLightMapData);
        File.WriteAllText(filePath + m_jsonFileName, json);
        AssetDatabase.Refresh();

        Debug.Log("Lightmaps saved");
    }

    private string[] CopyTextures(string directory, Texture2D[] textures)
    {
        string[] textureFileNames = new string[textures.Length];

        for (int i = 0; i < textures.Length; i++)
        {
            Texture2D texture = textures[i];
            textureFileNames[i] = texture.name;

            //Copy texture file to the directory
            FileUtil.ReplaceFile(AssetDatabase.GetAssetPath(texture), directory + Path.GetFileName(AssetDatabase.GetAssetPath(texture)));
            AssetDatabase.Refresh(); // Refresh so the texture file can be found and loaded
            Texture2D newTexture = Resources.Load<Texture2D>("LightMaps/" + SceneManager.GetActiveScene().name + "/" + m_folderName + "/" + texture.name); // Load the new texture as an object

            CopyTextureImporterProperties(textures[i], newTexture); // Ensure new texture takes on same properties as origional texture

            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(newTexture)); // Re-import texture file so it will be successfully compressed to desired format
            EditorUtility.CompressTexture(newTexture, textures[i].format, TextureCompressionQuality.Best); // Now compress the texture

            textures[i] = newTexture; // Set the new texture as the reference in the Json file
        }

        return textureFileNames;
    }

    private void CopyTextureImporterProperties(Texture2D fromTexture, Texture2D toTexture)
    {
        TextureImporter fromTextureImporter = GetTextureImporter(fromTexture);
        TextureImporter toTextureImporter = GetTextureImporter(toTexture);

        toTextureImporter.wrapMode = fromTextureImporter.wrapMode;
        toTextureImporter.anisoLevel = fromTextureImporter.anisoLevel;
        toTextureImporter.sRGBTexture = fromTextureImporter.sRGBTexture;
        toTextureImporter.textureType = fromTextureImporter.textureType;
        toTextureImporter.textureCompression = fromTextureImporter.textureCompression;
    }

    private TextureImporter GetTextureImporter(Texture2D texture)
    {
        string newTexturePath = AssetDatabase.GetAssetPath(texture);
        TextureImporter importer = AssetImporter.GetAtPath(newTexturePath) as TextureImporter;
        return importer;
    }

#endif

}