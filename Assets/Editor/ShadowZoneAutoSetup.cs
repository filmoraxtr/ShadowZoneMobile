// Assets/Editor/ShadowZoneAutoSetup.cs
// Unity Editor ilk açıldığında veya menüden çalıştırıldığında
// DemoMap sahnesini otomatik olarak oluşturur ve tüm bileşenleri bağlar.
// Menu: ShadowZone > 1. Setup Demo Scene

using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using TMPro;

[InitializeOnLoad]
public static class ShadowZoneAutoSetup
{
    private const string SETUP_KEY = "ShadowZone_SceneSetup_v1";
    private const string SCENE_PATH = "Assets/_Game/Scenes/DemoMap.unity";
    private const string PREFAB_DIR = "Assets/_Game/Prefabs";
    private const string SCENE_DIR = "Assets/_Game/Scenes";

    private static GameObject playerGO, cameraGO, cameraPivotGO;
    private static GameObject pistolGO, rifleGO;
    private static GameObject canvasGO, joystickBG, joystickHandle;
    private static GameObject fireBtn, reloadBtn, switchBtn, pauseBtn;
    private static GameObject healthBarGO;
    private static GameObject ammoUIGO, ammoTextGO, weaponNameTextGO, reloadingPanelGO;
    private static GameObject damageFlashGO, hudPanelGO;
    private static GameObject gameOverPanelGO, winPanelGO, pausePanelGO;
    private static GameObject scoreTextGO, waveTextGO, waveCountdownGO;
    private static GameObject gameOverScoreGO, winScoreGO;
    private static GameObject enemyGO, spawnParent, patrolParent, gameManagerGO;
    private static Transform[] spawnPoints, patrolPoints;

    static ShadowZoneAutoSetup()
    {
        if (!EditorPrefs.GetBool(SETUP_KEY, false))
        {
            EditorApplication.delayCall += RunAutoSetup;
        }
    }

    [MenuItem("ShadowZone/1. Setup Demo Scene (DemoMap)")]
    public static void RunAutoSetup()
    {
        if (!ConfirmSetup()) return;

        CreateDirectories();
        SetupTagsAndLayers();
        CreateScene();
        CreateGround();
        CreatePlayer();
        CreateCamera();
        CreateWeapons();
        CreateCanvas();
        CreateEnemy();
        CreateSpawnPoints();
        CreatePatrolPoints();
        CreateGameManager();
        WireAllReferences();
        FinalizeScene();

        EditorPrefs.SetBool(SETUP_KEY, true);

        Debug.Log("[ShadowZone] DemoMap kuruldu.");
    }

    [MenuItem("ShadowZone/Reset & Re-run Scene Setup")]
    public static void ResetSetup()
    {
        EditorPrefs.DeleteKey(SETUP_KEY);
        RunAutoSetup();
    }

    private static bool ConfirmSetup()
    {
        if (!EditorSceneManager.GetActiveScene().isDirty)
        {
            return true;
        }

        int choice = EditorUtility.DisplayDialogComplex(
            "ShadowZone Auto Setup",
            "Mevcut sahnede kaydedilmemiş değişiklikler var.",
            "Kaydet ve Devam",
            "Kaydetme, Devam",
            "İptal"
        );

        if (choice == 2) return false;

        if (choice == 0)
        {
            EditorSceneManager.SaveOpenScenes();
        }

        return true;
    }

    private static void CreateDirectories()
    {
        foreach (string dir in new[] { SCENE_DIR, PREFAB_DIR })
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        AssetDatabase.Refresh();
    }

    private static void SetupTagsAndLayers()
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");

        if (assets == null || assets.Length == 0)
        {
            Debug.LogWarning("[ShadowZone] TagManager.asset bulunamadı.");
            return;
        }

        SerializedObject tagManager = new SerializedObject(assets[0]);

        AddTagIfMissing(tagManager, "Head");
        AddTagIfMissing(tagManager, "Enemy");

        AddLayerIfMissing(tagManager, "Player", 8);
        AddLayerIfMissing(tagManager, "Enemy", 9);

        tagManager.ApplyModifiedProperties();
    }

    private static void AddTagIfMissing(SerializedObject tagManager, string tag)
    {
        SerializedProperty tags = tagManager.FindProperty("tags");

        if (tags == null) return;

        for (int i = 0; i < tags.arraySize; i++)
        {
            if (tags.GetArrayElementAtIndex(i).stringValue == tag)
            {
                return;
            }
        }

        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
    }

    private static void AddLayerIfMissing(SerializedObject tagManager, string layerName, int slotIndex)
    {
        SerializedProperty layers = tagManager.FindProperty("layers");

        if (layers == null || layers.arraySize <= slotIndex)
        {
            return;
        }

        SerializedProperty slot = layers.GetArrayElementAtIndex(slotIndex);

        if (string.IsNullOrEmpty(slot.stringValue))
        {
            slot.stringValue = layerName;
        }
    }

    private static void CreateScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject lightGO = new GameObject("Directional Light");
        Light light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        light.shadows = LightShadows.Soft;

        lightGO.transform.SetPositionAndRotation(
            new Vector3(0f, 10f, 0f),
            Quaternion.Euler(50f, -30f, 0f)
        );

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    private static void CreateGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(10f, 1f, 10f);
        ground.isStatic = true;

        CreateWall("Wall_N", new Vector3(0f, 2f, 50f), new Vector3(101f, 4f, 1f));
        CreateWall("Wall_S", new Vector3(0f, 2f, -50f), new Vector3(101f, 4f, 1f));
        CreateWall("Wall_E", new Vector3(50f, 2f, 0f), new Vector3(1f, 4f, 101f));
        CreateWall("Wall_W", new Vector3(-50f, 2f, 0f), new Vector3(1f, 4f, 101f));

        CreateCover("Cover_A", new Vector3(12f, 0.75f, 10f));
        CreateCover("Cover_B", new Vector3(-15f, 0.75f, 8f));
        CreateCover("Cover_C", new Vector3(6f, 0.75f, -13f));
        CreateCover("Cover_D", new Vector3(-9f, 0.75f, -19f));
    }

    private static void CreateWall(string name, Vector3 position, Vector3 scale)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.position = position;
        wall.transform.localScale = scale;
        wall.isStatic = true;
    }

    private static void CreateCover(string name, Vector3 position)
    {
        GameObject cover = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cover.name = name;
        cover.transform.position = position;
        cover.transform.localScale = new Vector3(3f, 1.5f, 1.5f);
        cover.isStatic = true;
    }

    private static void CreatePlayer()
    {
        playerGO = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        playerGO.name = "Player";
        playerGO.tag = "Player";
        playerGO.layer = 8;
        playerGO.transform.position = new Vector3(0f, 1f, 0f);

        CapsuleCollider capsuleCollider = playerGO.GetComponent<CapsuleCollider>();

        if (capsuleCollider != null)
        {
            Object.DestroyImmediate(capsuleCollider);
        }

        CharacterController characterController = playerGO.AddComponent<CharacterController>();
        characterController.center = Vector3.zero;
        characterController.radius = 0.4f;
        characterController.height = 2f;

        playerGO.AddComponent<PlayerHealth>();
        playerGO.AddComponent<ThirdPersonPlayerController>();
        playerGO.AddComponent<WeaponManager>();

        cameraPivotGO = new GameObject("CameraPivot");
        cameraPivotGO.transform.SetParent(playerGO.transform, false);
        cameraPivotGO.transform.localPosition = new Vector3(0f, 1.5f, 0f);
    }

    private static void CreateCamera()
    {
        cameraGO = new GameObject("Main Camera");
        cameraGO.tag = "MainCamera";

        Camera camera = cameraGO.AddComponent<Camera>();
        camera.fieldOfView = 75f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 500f;

        cameraGO.AddComponent<AudioListener>();
        cameraGO.AddComponent<TouchCameraController>();

        cameraGO.transform.SetPositionAndRotation(
            new Vector3(0.5f, 2.5f, -3f),
            Quaternion.Euler(15f, 0f, 0f)
        );
    }

    private static void CreateWeapons()
    {
        GameObject holder = new GameObject("WeaponHolder");
        holder.transform.SetParent(playerGO.transform, false);
        holder.transform.localPosition = new Vector3(0f, 1.4f, 0.4f);

        pistolGO = CreateWeaponGO(
            "Pistol",
            holder,
            new Vector3(0.08f, 0.14f, 0.25f),
            new Vector3(0.2f, 0f, 0.1f),
            new Vector3(0.2f, 0f, 0.3f)
        );

        rifleGO = CreateWeaponGO(
            "Rifle",
            holder,
            new Vector3(0.06f, 0.11f, 0.60f),
            new Vector3(0.2f, 0f, 0.25f),
            new Vector3(0.2f, 0f, 0.65f)
        );

        rifleGO.SetActive(false);
    }

    private static GameObject CreateWeaponGO(
        string weaponName,
        GameObject parent,
        Vector3 modelScale,
        Vector3 modelOffset,
        Vector3 muzzleOffset
    )
    {
        GameObject root = new GameObject(weaponName);
        root.transform.SetParent(parent.transform, false);

        GameObject model = GameObject.CreatePrimitive(PrimitiveType.Cube);
        model.name = weaponName + "Model";
        model.transform.SetParent(root.transform, false);
        model.transform.localScale = modelScale;
        model.transform.localPosition = modelOffset;

        BoxCollider boxCollider = model.GetComponent<BoxCollider>();

        if (boxCollider != null)
        {
            Object.DestroyImmediate(boxCollider);
        }

        GameObject muzzle = new GameObject("MuzzlePoint");
        muzzle.transform.SetParent(root.transform, false);
        muzzle.transform.localPosition = muzzleOffset;

        AudioSource audioSource = root.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        root.AddComponent<WeaponBase>();

        return root;
    }

    private static void CreateCanvas()
    {
        canvasGO = new GameObject("Canvas");

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        hudPanelGO = MakeEmpty("HUDPanel", canvasGO);
        RectTransform hudRect = hudPanelGO.GetComponent<RectTransform>();

        if (hudRect == null)
        {
            hudRect = hudPanelGO.AddComponent<RectTransform>();
        }

        SetStretchFull(hudRect);

        CreateJoystick();
        CreateButtons();
        CreateHealthBar();
        CreateAmmoUI();
        CreateInfoTexts();
        CreateDamageFlash();
        CreateGamePanels();
    }

    private static void CreateJoystick()
    {
        joystickBG = new GameObject("JoystickBG");
        joystickBG.transform.SetParent(hudPanelGO.transform, false);

        Image bgImage = joystickBG.AddComponent<Image>();
        bgImage.color = new Color(0.8f, 0.8f, 0.8f, 0.25f);
        bgImage.raycastTarget = true;

        RectTransform bgRect = joystickBG.GetComponent<RectTransform>();
        SetAnchorBL(bgRect, new Vector2(200f, 220f), new Vector2(280f, 280f));

        joystickBG.AddComponent<MobileJoystick>();

        joystickHandle = new GameObject("JoystickHandle");
        joystickHandle.transform.SetParent(joystickBG.transform, false);

        Image handleImage = joystickHandle.AddComponent<Image>();
        handleImage.color = new Color(1f, 1f, 1f, 0.55f);
        handleImage.raycastTarget = false;

        RectTransform handleRect = joystickHandle.GetComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0.5f, 0.5f);
        handleRect.anchorMax = new Vector2(0.5f, 0.5f);
        handleRect.pivot = new Vector2(0.5f, 0.5f);
        handleRect.anchoredPosition = Vector2.zero;
        handleRect.sizeDelta = new Vector2(110f, 110f);
    }

    private static void CreateButtons()
    {
        fireBtn = MakeButton(
            "FireButton",
            hudPanelGO,
            new Vector2(-210f, 200f),
            new Vector2(240f, 240f),
            "FIRE",
            new Color(0.9f, 0.2f, 0.2f, 0.55f)
        );

        fireBtn.AddComponent<EventTrigger>();

        reloadBtn = MakeButton(
            "ReloadButton",
            hudPanelGO,
            new Vector2(-390f, 420f),
            new Vector2(155f, 155f),
            "R",
            new Color(0.2f, 0.6f, 0.9f, 0.55f)
        );

        switchBtn = MakeButton(
            "SwitchButton",
            hudPanelGO,
            new Vector2(-565f, 420f),
            new Vector2(155f, 155f),
            "⇄",
            new Color(0.2f, 0.85f, 0.4f, 0.55f)
        );

        pauseBtn = MakeButton(
            "PauseButton",
            hudPanelGO,
            Vector2.zero,
            new Vector2(90f, 90f),
            "II",
            new Color(0.5f, 0.5f, 0.5f, 0.55f)
        );

        RectTransform pauseRect = pauseBtn.GetComponent<RectTransform>();
        pauseRect.anchorMin = new Vector2(1f, 1f);
        pauseRect.anchorMax = new Vector2(1f, 1f);
        pauseRect.pivot = new Vector2(1f, 1f);
        pauseRect.anchoredPosition = new Vector2(-20f, -20f);
        pauseRect.sizeDelta = new Vector2(90f, 90f);
    }

    private static void CreateHealthBar()
    {
        healthBarGO = new GameObject("HealthBar");
        healthBarGO.transform.SetParent(hudPanelGO.transform, false);

        Slider slider = healthBarGO.AddComponent<Slider>();
        slider.interactable = false;
        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.value = 100f;

        RectTransform rect = healthBarGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(20f, -20f);
        rect.sizeDelta = new Vector2(500f, 50f);

        GameObject background = MakeImageChild(
            "Background",
            healthBarGO,
            new Color(0.1f, 0.1f, 0.1f, 0.6f),
            false
        );

        SetStretchFull(background.GetComponent<RectTransform>());

        GameObject fillArea = MakeEmpty("Fill Area", healthBarGO);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(4f, 4f);
        fillAreaRect.offsetMax = new Vector2(-4f, -4f);

        GameObject fill = MakeImageChild(
            "Fill",
            fillArea,
            new Color(0.15f, 0.9f, 0.15f, 1f),
            false
        );

        RectTransform fillRect = fill.GetComponent<RectTransform>();
        SetStretchFull(fillRect);
        fillRect.offsetMax = Vector2.zero;

        slider.fillRect = fillRect;
    }

    private static void CreateAmmoUI()
    {
        ammoUIGO = MakeEmpty("AmmoUI", hudPanelGO);

        RectTransform ammoRect = ammoUIGO.AddComponent<RectTransform>();
        ammoRect.anchorMin = new Vector2(1f, 0f);
        ammoRect.anchorMax = new Vector2(1f, 0f);
        ammoRect.pivot = new Vector2(1f, 0f);
        ammoRect.anchoredPosition = new Vector2(-20f, 130f);
        ammoRect.sizeDelta = new Vector2(350f, 120f);

        ammoUIGO.AddComponent<AmmoUI>();

        weaponNameTextGO = MakeTMP(
            "WeaponNameText",
            ammoUIGO,
            "PISTOL",
            28f,
            TextAlignmentOptions.Right,
            Color.yellow
        );

        RectTransform weaponNameRect = weaponNameTextGO.GetComponent<RectTransform>();
        weaponNameRect.anchorMin = new Vector2(0f, 1f);
        weaponNameRect.anchorMax = new Vector2(1f, 1f);
        weaponNameRect.pivot = new Vector2(0.5f, 1f);
        weaponNameRect.offsetMin = Vector2.zero;
        weaponNameRect.offsetMax = Vector2.zero;
        weaponNameRect.sizeDelta = new Vector2(0f, 40f);

        ammoTextGO = MakeTMP(
            "AmmoText",
            ammoUIGO,
            "12 / 96",
            54f,
            TextAlignmentOptions.Right,
            Color.white
        );

        RectTransform ammoTextRect = ammoTextGO.GetComponent<RectTransform>();
        ammoTextRect.anchorMin = new Vector2(0f, 0f);
        ammoTextRect.anchorMax = new Vector2(1f, 0f);
        ammoTextRect.pivot = new Vector2(0.5f, 0f);
        ammoTextRect.offsetMin = Vector2.zero;
        ammoTextRect.offsetMax = Vector2.zero;
        ammoTextRect.sizeDelta = new Vector2(0f, 70f);

        reloadingPanelGO = MakeImageChild(
            "ReloadingPanel",
            hudPanelGO,
            new Color(0f, 0f, 0f, 0.72f),
            false
        );

        RectTransform reloadPanelRect = reloadingPanelGO.GetComponent<RectTransform>();
        reloadPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
        reloadPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
        reloadPanelRect.pivot = new Vector2(0.5f, 0.5f);
        reloadPanelRect.anchoredPosition = new Vector2(0f, -100f);
        reloadPanelRect.sizeDelta = new Vector2(420f, 70f);

        GameObject reloadText = MakeTMP(
            "ReloadingText",
            reloadingPanelGO,
            "RELOADING...",
            36f,
            TextAlignmentOptions.Center,
            Color.white
        );

        SetStretchFull(reloadText.GetComponent<RectTransform>());
        reloadingPanelGO.SetActive(false);
    }

    private static void CreateInfoTexts()
    {
        scoreTextGO = MakeTMP(
            "ScoreText",
            hudPanelGO,
            "SCORE: 0",
            32f,
            TextAlignmentOptions.Left,
            Color.white
        );

        RectTransform scoreRect = scoreTextGO.GetComponent<RectTransform>();
        scoreRect.anchorMin = new Vector2(0f, 1f);
        scoreRect.anchorMax = new Vector2(0f, 1f);
        scoreRect.pivot = new Vector2(0f, 1f);
        scoreRect.anchoredPosition = new Vector2(20f, -75f);
        scoreRect.sizeDelta = new Vector2(320f, 45f);

        waveTextGO = MakeTMP(
            "WaveText",
            hudPanelGO,
            "WAVE 1 / 3",
            32f,
            TextAlignmentOptions.Left,
            Color.white
        );

        RectTransform waveRect = waveTextGO.GetComponent<RectTransform>();
        waveRect.anchorMin = new Vector2(0f, 1f);
        waveRect.anchorMax = new Vector2(0f, 1f);
        waveRect.pivot = new Vector2(0f, 1f);
        waveRect.anchoredPosition = new Vector2(20f, -122f);
        waveRect.sizeDelta = new Vector2(320f, 45f);

        waveCountdownGO = MakeTMP(
            "WaveCountdownText",
            canvasGO,
            "WAVE 1",
            85f,
            TextAlignmentOptions.Center,
            Color.yellow
        );

        RectTransform countdownRect = waveCountdownGO.GetComponent<RectTransform>();
        countdownRect.anchorMin = new Vector2(0.5f, 0.5f);
        countdownRect.anchorMax = new Vector2(0.5f, 0.5f);
        countdownRect.pivot = new Vector2(0.5f, 0.5f);
        countdownRect.anchoredPosition = ne
