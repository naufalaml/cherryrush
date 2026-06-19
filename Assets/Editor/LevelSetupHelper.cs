using UnityEngine;
using UnityEditor;
using System.IO;

public class LevelSetupHelper : EditorWindow
{
    private const string ASET_DIR = "Assets/aset";

    [MenuItem("Cherry Rush/Setup Demo Level")]
    public static void SetupDemoLevel()
    {
        // 1. Buat folder aset jika belum ada
        if (!Directory.Exists(ASET_DIR))
        {
            Directory.CreateDirectory(ASET_DIR);
        }

        // 2. Generate placeholder sprites if they don't exist
        GeneratePlaceholders();

        // 3. Cari Main Camera
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.transform.position = new Vector3(0, 1, -10);
            mainCam.orthographic = true;
            mainCam.orthographicSize = 6;
            
            // Pasang CameraFollow
            if (mainCam.GetComponent<CameraFollow>() == null)
            {
                mainCam.gameObject.AddComponent<CameraFollow>();
            }

            // Tempelkan Background ke Camera agar terus mengikuti pergerakan player
            GameObject bgObj = GameObject.Find("Backgroundd_0");
            if (bgObj != null)
            {
                // HAPUS COLLIDER JIKA ADA PADA BACKGROUND (karena dapat menabrak player & membuat player mental)
                Collider2D bgCollider = bgObj.GetComponent<Collider2D>();
                if (bgCollider != null)
                {
                    DestroyImmediate(bgCollider);
                }

                bgObj.transform.SetParent(mainCam.transform);
                bgObj.transform.localPosition = new Vector3(0f, 0f, 10f); // Taruh di belakang rendering game
                
                // Set scale agar menutupi layar dengan pas
                bgObj.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
            }
        }

        // 4. Buat Parent Object untuk Level
        GameObject levelParent = GameObject.Find("_LevelLayout");
        if (levelParent != null)
        {
            DestroyImmediate(levelParent); // Hapus level lama jika ada
        }
        levelParent = new GameObject("_LevelLayout");

        // Load Sprites
        Sprite groundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ASET_DIR + "/ground_placeholder.png");
        Sprite playerSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ASET_DIR + "/player_sprite.png");
        if (playerSprite == null)
        {
            playerSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ASET_DIR + "/player_placeholder.png");
        }
        Sprite cherrySprite = AssetDatabase.LoadAssetAtPath<Sprite>(ASET_DIR + "/cherry_placeholder.png");
        Sprite spikeSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ASET_DIR + "/spike_placeholder.png");
        Sprite flagSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ASET_DIR + "/flag_placeholder.png");

        // 5. Buat Player
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            DestroyImmediate(playerObj);
        }
        playerObj = new GameObject("Player");
        playerObj.tag = "Player";
        playerObj.transform.position = new Vector3(-8f, -1.5f, 0f);

        SpriteRenderer playerSR = playerObj.AddComponent<SpriteRenderer>();
        playerSR.sprite = playerSprite;
        playerSR.sortingOrder = 10;

        Rigidbody2D playerRB = playerObj.AddComponent<Rigidbody2D>();
        playerRB.constraints = RigidbodyConstraints2D.FreezeRotation;
        playerRB.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CapsuleCollider2D playerCol = playerObj.AddComponent<CapsuleCollider2D>();
        playerCol.size = new Vector2(0.8f, 0.95f);

        PlayerController playerCtrl = playerObj.AddComponent<PlayerController>();
        playerCtrl.groundLayer = 1 << 0; // Layer Default

        playerObj.AddComponent<PlayerAnimator>();

        // Set camera target ke player
        if (mainCam != null)
        {
            CameraFollow camFollow = mainCam.GetComponent<CameraFollow>();
            if (camFollow != null)
            {
                camFollow.target = playerObj.transform;
            }
        }

        // 6. Buat Platform & Rintangan (Sesuai Desain 6 Area)

        // Area 1: Area Awal (Learning Area)
        CreatePlatform(levelParent, "Ground_Area1", new Vector3(-7f, -2.5f, 0f), new Vector3(8f, 1f, 1f), groundSprite);

        // Area 2: Double Jump Area
        CreatePlatform(levelParent, "Ground_Area2_Base", new Vector3(0.5f, -2.5f, 0f), new Vector3(7f, 1f, 1f), groundSprite);
        CreatePlatform(levelParent, "Platform_Jump1", new Vector3(-1f, -0.5f, 0f), new Vector3(2.5f, 0.5f, 1f), groundSprite);
        CreatePlatform(levelParent, "Platform_Jump2", new Vector3(2f, 1.5f, 0f), new Vector3(2.5f, 0.5f, 1f), groundSprite);

        // Area 3: Item Speed Boost
        CreatePlatform(levelParent, "Ground_Area3_Base", new Vector3(7.5f, -1.5f, 0f), new Vector3(7f, 1f, 1f), groundSprite);
        // Buat Item Cherry
        GameObject cherryObj = new GameObject("CherryItem");
        cherryObj.transform.SetParent(levelParent.transform);
        cherryObj.transform.position = new Vector3(7.5f, -0.5f, 0f);
        SpriteRenderer cherrySR = cherryObj.AddComponent<SpriteRenderer>();
        cherrySR.sprite = cherrySprite;
        BoxCollider2D cherryCol = cherryObj.AddComponent<BoxCollider2D>();
        cherryCol.isTrigger = true;
        cherryObj.AddComponent<SpeedBoostItem>();

        // Area 4: Area Kombinasi (Inti Level - Jurang Lebar)
        CreatePlatform(levelParent, "Ground_Area4_Start", new Vector3(15.5f, -1.5f, 0f), new Vector3(9f, 1f, 1f), groundSprite);
        // Jurang berada di antara x = 20 dan x = 27
        // Tempatkan duri di dasar jurang
        CreatePlatform(levelParent, "Ground_Area4_End", new Vector3(31f, -1.5f, 0f), new Vector3(8f, 1f, 1f), groundSprite);
        
        // Duri di bawah jurang
        for (float x = 20.5f; x <= 26.5f; x += 1f)
        {
            CreateSpike(levelParent, new Vector3(x, -5f, 0f), spikeSprite);
        }
        // Platform jebakan di dasar jurang (sebagai visual/tambahan agar tidak kosong)
        CreatePlatform(levelParent, "Pit_Floor", new Vector3(23.5f, -5.5f, 0f), new Vector3(8f, 1f, 1f), groundSprite);

        // Area 5: Area Presisi (Challenge)
        // Spikes di sepanjang lantai dasar
        CreatePlatform(levelParent, "Ground_Area5_BaseSpikes", new Vector3(40.5f, -3.5f, 0f), new Vector3(11f, 1f, 1f), groundSprite);
        for (float x = 36.5f; x <= 44.5f; x += 1f)
        {
            CreateSpike(levelParent, new Vector3(x, -2.7f, 0f), spikeSprite);
        }

        // Platform gantung kecil
        CreatePlatform(levelParent, "Platform_Precision1", new Vector3(36.5f, -0.5f, 0f), new Vector3(1.5f, 0.4f, 1f), groundSprite);
        CreatePlatform(levelParent, "Platform_Precision2", new Vector3(40.5f, 1.0f, 0f), new Vector3(1.5f, 0.4f, 1f), groundSprite);
        CreatePlatform(levelParent, "Platform_Precision3", new Vector3(44.5f, -0.5f, 0f), new Vector3(1.5f, 0.4f, 1f), groundSprite);

        // Rintangan bergerak (Moving Spike Ball)
        GameObject movingSpike = CreateSpike(levelParent, new Vector3(40.5f, 2.5f, 0f), spikeSprite);
        movingSpike.name = "MovingSpike";
        MovingHazard mover = movingSpike.AddComponent<MovingHazard>();
        mover.direction = MovingHazard.MovementDirection.Horizontal;
        mover.speed = 2.5f;
        mover.distance = 3.5f;

        // Area 6: Finish Area
        CreatePlatform(levelParent, "Ground_Finish", new Vector3(53f, -2.5f, 0f), new Vector3(8f, 1f, 1f), groundSprite);
        
        // Finish Flag
        GameObject flagObj = new GameObject("FinishFlag");
        flagObj.transform.SetParent(levelParent.transform);
        flagObj.transform.position = new Vector3(53f, -1.3f, 0f);
        SpriteRenderer flagSR = flagObj.AddComponent<SpriteRenderer>();
        flagSR.sprite = flagSprite;
        BoxCollider2D flagCol = flagObj.AddComponent<BoxCollider2D>();
        flagCol.isTrigger = true;
        flagObj.AddComponent<FinishFlag>();

        // Log informasi ke console
        Debug.Log("Level Cherry Rush Adventure berhasil disetup! Silakan tekan tombol Play untuk mencoba.");
    }

    private static void CreatePlatform(GameObject parent, string name, Vector3 pos, Vector3 scale, Sprite sprite)
    {
        GameObject plat = new GameObject(name);
        plat.transform.SetParent(parent.transform);
        plat.transform.position = pos;
        plat.transform.localScale = scale;

        SpriteRenderer sr = plat.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.drawMode = SpriteDrawMode.Sliced; // Mendukung ukuran berapapun tanpa distorsi
        sr.size = Vector2.one;

        BoxCollider2D col = plat.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;
    }

    private static GameObject CreateSpike(GameObject parent, Vector3 pos, Sprite sprite)
    {
        GameObject spike = new GameObject("Spike");
        spike.transform.SetParent(parent.transform);
        spike.transform.position = pos;

        SpriteRenderer sr = spike.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;

        PolygonCollider2D col = spike.AddComponent<PolygonCollider2D>();
        col.isTrigger = true; // Duri memicu trigger reload scene saat ditabrak

        spike.AddComponent<Hazard>();

        return spike;
    }

    private static void GeneratePlaceholders()
    {
        // Ground placeholder (32x32 pixels) - Hijau rumput dengan tanah cokelat
        string groundPath = ASET_DIR + "/ground_placeholder.png";
        if (!File.Exists(groundPath))
        {
            Texture2D tex = new Texture2D(32, 32);
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    if (y >= 26) // Bagian rumput
                        tex.SetPixel(x, y, new Color(0.2f, 0.7f, 0.2f));
                    else if (y >= 23) // Transisi rumput-tanah
                        tex.SetPixel(x, y, new Color(0.15f, 0.6f, 0.15f));
                    else // Tanah cokelat
                        tex.SetPixel(x, y, new Color(0.45f, 0.25f, 0.1f));
                }
            }
            tex.Apply();
            SaveTextureAsPNG(tex, groundPath);
        }

        // Player placeholder (32x32 pixels) - Karakter biru bulat imut dengan mata
        string playerPath = ASET_DIR + "/player_placeholder.png";
        if (!File.Exists(playerPath))
        {
            Texture2D tex = new Texture2D(32, 32);
            // Bersihkan dengan transparan
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                    tex.SetPixel(x, y, Color.clear);

            // Gambar badan bulat biru
            Vector2 center = new Vector2(16, 16);
            float radius = 12f;
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    if (Vector2.Distance(new Vector2(x, y), center) <= radius)
                    {
                        tex.SetPixel(x, y, new Color(0.1f, 0.5f, 0.9f));
                    }
                }
            }
            // Gambar mata putih dan hitam (menghadap kanan)
            tex.SetPixel(20, 18, Color.white);
            tex.SetPixel(21, 18, Color.white);
            tex.SetPixel(20, 19, Color.white);
            tex.SetPixel(21, 19, Color.white);
            tex.SetPixel(21, 18, Color.black); // Pupil

            tex.Apply();
            SaveTextureAsPNG(tex, playerPath);
        }

        // Cherry placeholder (16x16 pixels) - Buah ceri merah dengan daun hijau
        string cherryPath = ASET_DIR + "/cherry_placeholder.png";
        if (!File.Exists(cherryPath))
        {
            Texture2D tex = new Texture2D(16, 16);
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                    tex.SetPixel(x, y, Color.clear);

            // Bulatan buah ceri merah
            Vector2 c1 = new Vector2(6, 5);
            Vector2 c2 = new Vector2(10, 5);
            float r = 3f;
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if (Vector2.Distance(new Vector2(x, y), c1) <= r || Vector2.Distance(new Vector2(x, y), c2) <= r)
                    {
                        tex.SetPixel(x, y, new Color(0.9f, 0.1f, 0.1f));
                    }
                }
            }
            // Batang dan daun hijau
            tex.SetPixel(6, 8, new Color(0.1f, 0.6f, 0.1f));
            tex.SetPixel(7, 9, new Color(0.1f, 0.6f, 0.1f));
            tex.SetPixel(8, 10, new Color(0.1f, 0.6f, 0.1f));
            tex.SetPixel(9, 10, new Color(0.1f, 0.6f, 0.1f));
            tex.SetPixel(10, 9, new Color(0.1f, 0.6f, 0.1f));
            tex.SetPixel(10, 8, new Color(0.1f, 0.6f, 0.1f));
            tex.SetPixel(11, 11, new Color(0.2f, 0.8f, 0.2f)); // Daun

            tex.Apply();
            SaveTextureAsPNG(tex, cherryPath);
        }

        // Spike placeholder (16x16 pixels) - Segitiga duri abu-abu metalik
        string spikePath = ASET_DIR + "/spike_placeholder.png";
        if (!File.Exists(spikePath))
        {
            Texture2D tex = new Texture2D(16, 16);
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                    tex.SetPixel(x, y, Color.clear);

            // Segitiga duri
            for (int y = 0; y < 16; y++)
            {
                int widthAtY = y; // Duri melebar di bagian bawah
                int startX = 8 - (widthAtY / 2);
                int endX = 8 + (widthAtY / 2);
                for (int x = startX; x <= endX; x++)
                {
                    tex.SetPixel(x, y, new Color(0.6f, 0.6f, 0.65f));
                }
            }

            tex.Apply();
            SaveTextureAsPNG(tex, spikePath);
        }

        // Flag placeholder (16x32 pixels) - Tiang cokelat dengan bendera merah segitiga
        string flagPath = ASET_DIR + "/flag_placeholder.png";
        if (!File.Exists(flagPath))
        {
            Texture2D tex = new Texture2D(16, 32);
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 16; x++)
                    tex.SetPixel(x, y, Color.clear);

            // Tiang bendera cokelat (x = 4)
            for (int y = 0; y < 32; y++)
            {
                tex.SetPixel(4, y, new Color(0.5f, 0.3f, 0.1f));
            }
            // Bendera merah (segitiga dari y = 18 sampai 30, mengarah ke kanan)
            for (int y = 18; y <= 30; y++)
            {
                int flagWidth = 30 - y; // Bendera menyusut ke atas dan bawah
                if (y < 24) flagWidth = y - 18; // Lebar bertambah
                
                for (int x = 5; x < 5 + (flagWidth * 2); x++)
                {
                    if (x < 16)
                    {
                        tex.SetPixel(x, y, new Color(0.9f, 0.1f, 0.1f));
                    }
                }
            }

            tex.Apply();
            SaveTextureAsPNG(tex, flagPath);
        }

        string customPlayerPath = ASET_DIR + "/player_sprite.png";
        if (File.Exists(customPlayerPath))
        {
            TextureImporter importer = AssetImporter.GetAtPath(customPlayerPath) as TextureImporter;
            if (importer != null && (importer.textureType != TextureImporterType.Sprite || importer.filterMode != FilterMode.Point))
            {
                importer.filterMode = FilterMode.Point;
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 16;
                importer.SaveAndReimport();
            }
        }

        AssetDatabase.Refresh();
    }

    private static void SaveTextureAsPNG(Texture2D texture, string path)
    {
        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(path, bytes);
        
        // Konfigurasi Texture Importer agar pixel art tidak blur
        AssetDatabase.ImportAsset(path);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.filterMode = FilterMode.Point;
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 16; // 16 pixel = 1 unit Unity agar ukurannya pas
            importer.SaveAndReimport();
        }
    }
}
