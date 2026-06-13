// Assets/Editor/AndroidBuildSettingsSetup.cs
// Unity Editor ilk açıldığında veya menüden çalıştırıldığında
// Shadow Zone Mobile için tüm Android build ayarlarını uygular.
// Menu: ShadowZone > 2. Setup Android Build Settings

using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[InitializeOnLoad]
public static class AndroidBuildSettingsSetup
{
    private const string SETUP_KEY = "ShadowZone_AndroidSetup_v1";

    static AndroidBuildSettingsSetup()
    {
        if (!EditorPrefs.GetBool(SETUP_KEY, false))
        {
            EditorApplication.delayCall += RunAutoSetup;
        }
    }

    [MenuItem("ShadowZone/2. Setup Android Build Settings")]
    public static void RunAutoSetup()
    {
        ApplySettings();
        EditorPrefs.SetBool(SETUP_KEY, true);
    }

    [MenuItem("ShadowZone/Reset Android Settings")]
    public static void ResetSetup()
    {
        EditorPrefs.DeleteKey(SETUP_KEY);
        RunAutoSetup();
    }

    private static void ApplySettings()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            bool switchNow = EditorUtility.DisplayDialog(
                "ShadowZone — Platform Switch",
                "Aktif platform Android değil.\nAndroid'e geçmek istiyor musun?\nBu işlem biraz sürebilir.",
                "Evet, Geç",
                "Şimdi Değil"
            );

            if (switchNow)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(
                    BuildTargetGroup.Android,
                    BuildTarget.Android
                );
            }
        }

        PlayerSettings.companyName = "FilmoraXTR";
        PlayerSettings.productName = "Shadow Zone Mobile";
        PlayerSettings.bundleVersion = "0.1.0";

        PlayerSettings.SetApplicationIdentifier(
            BuildTargetGroup.Android,
            "com.filmoraxtr.shadowzonemobile"
        );

        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.Android.bundleVersionCode = 1;
        PlayerSettings.Android.preferredInstallLocation = AndroidPreferredInstallLocation.Auto;

        PlayerSettings.Android.optimizedFramePacing = true;

        PlayerSettings.SetScriptingBackend(
            BuildTargetGroup.Android,
            ScriptingImplementation.IL2CPP
        );

        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);

        PlayerSettings.SetGraphicsAPIs(
            BuildTarget.Android,
            new[]
            {
                GraphicsDeviceType.Vulkan,
                GraphicsDeviceType.OpenGLES3
            }
        );

        PlayerSettings.SetMobileMTRendering(BuildTargetGroup.Android, true);

        EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;

        PlayerSettings.Android.forceInternetPermission = false;
        PlayerSettings.Android.forceSDCardPermission = false;
        PlayerSettings.Android.disableDepthAndStencilAfterResolve = false;

        AssetDatabase.SaveAssets();

        Debug.Log("[ShadowZone] Android build ayarları uygulandı.");
        Debug.Log("Package: com.filmoraxtr.shadowzonemobile");
        Debug.Log("Product: Shadow Zone Mobile");
        Debug.Log("Company: FilmoraXTR");
        Debug.Log("Version: 0.1.0");
        Debug.Log("Backend: IL2CPP");
        Debug.Log("Architecture: ARM64");
        Debug.Log("Min API: Android 7.0 / API 24");
        Debug.Log("Graphics API: Vulkan + OpenGLES3");
        Debug.Log("Orientation: Landscape Left");
    }
}
