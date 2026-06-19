using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Collections.Generic;

public class SetupRealFoxAnimation : EditorWindow
{
    private const string INPUT_PATH = "Assets/aset/fox_real_spritesheet.jpg";
    private const string OUTPUT_PATH = "Assets/aset/fox_real_spritesheet_transparent.png";

    [MenuItem("Cherry Rush/Setup Real Fox Animation")]
    public static void SetupFoxAnimation()
    {
        if (!File.Exists(INPUT_PATH))
        {
            Debug.LogError("Sprite sheet input tidak ditemukan di " + INPUT_PATH + ". Pastikan Anda telah menaruh gambarnya.");
            return;
        }

        // 1. Pastikan file input bisa dibaca (isReadable = true)
        ConfigureTextureReadable(INPUT_PATH, true);

        // 2. Proses penghapusan background cyan, tulisan teks, dan garis pembatas hitam
        ProcessTextureCleanUp();

        // 3. Potong Sprite Sheet secara dinamis berdasarkan baris dan kolom yang bervariasi
        SliceCleanSpriteSheet();

        // 4. Muat ulang database aset Unity agar potongan sprite baru terbaca
        AssetDatabase.ImportAsset(OUTPUT_PATH);
        AssetDatabase.Refresh();

        // Ambil hasil potongan sprite
        object[] assets = AssetDatabase.LoadAllAssetsAtPath(OUTPUT_PATH);
        List<Sprite> idleSprites = new List<Sprite>();
        List<Sprite> walkSprites = new List<Sprite>();
        List<Sprite> jumpSprites = new List<Sprite>();
        List<Sprite> doubleJumpSprites = new List<Sprite>();

        foreach (var asset in assets)
        {
            if (asset is Sprite sprite)
            {
                if (sprite.name.Contains("idle"))
                    idleSprites.Add(sprite);
                else if (sprite.name.Contains("walk"))
                    walkSprites.Add(sprite);
                else if (sprite.name.Contains("jump"))
                    jumpSprites.Add(sprite);
                else if (sprite.name.Contains("djump"))
                    doubleJumpSprites.Add(sprite);
            }
        }

        // Urutkan berdasarkan nama agar frame runut
        idleSprites.Sort((a, b) => a.name.CompareTo(b.name));
        walkSprites.Sort((a, b) => a.name.CompareTo(b.name));
        jumpSprites.Sort((a, b) => a.name.CompareTo(b.name));
        doubleJumpSprites.Sort((a, b) => a.name.CompareTo(b.name));

        if (idleSprites.Count == 0 || walkSprites.Count == 0)
        {
            Debug.LogError("Gagal memuat potongan sprite. Pastikan proses slicing berhasil.");
            return;
        }

        // 5. Buat file .anim (Animation Clip)
        AnimationClip idleClip = CreateAnimationClip(idleSprites, "Fox_Idle", true, 6); // 6 FPS untuk idle agar rileks
        AnimationClip walkClip = CreateAnimationClip(walkSprites, "Fox_Walk", true, 10); // 10 FPS untuk jalan
        AnimationClip jumpClip = CreateAnimationClip(jumpSprites, "Fox_Jump", false, 8); // 8 FPS
        AnimationClip doubleJumpClip = CreateAnimationClip(doubleJumpSprites, "Fox_DoubleJump", false, 10); // 10 FPS

        // 6. Buat Animator Controller
        AnimatorController animatorController = CreateAnimatorController(idleClip, walkClip, jumpClip, doubleJumpClip);

        // 7. Cari Player di Scene & Pasang Komponen Animator
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj == null)
        {
            playerObj = GameObject.Find("player_sprite-removebg-preview_0");
        }

        if (playerObj != null)
        {
            // Matikan PlayerAnimator prosedural lama jika terpasang
            var oldProcAnim = playerObj.GetComponent<PlayerAnimator>();
            if (oldProcAnim != null)
            {
                DestroyImmediate(oldProcAnim);
            }

            Animator animator = playerObj.GetComponent<Animator>();
            if (animator == null)
            {
                animator = playerObj.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = animatorController;

            // Atur sprite awal karakter di SpriteRenderer
            SpriteRenderer sr = playerObj.GetComponent<SpriteRenderer>();
            if (sr != null && idleSprites.Count > 0)
            {
                sr.sprite = idleSprites[0];
            }

            Debug.Log("Traditional Fox Animation berhasil di-setup pada " + playerObj.name + "! Silakan tekan Play.");
        }
        else
        {
            Debug.LogWarning("Karakter Player tidak ditemukan di scene. Animator Controller telah dibuat di Assets/aset/Player_Controller.controller.");
        }
    }

    private static void ConfigureTextureReadable(string path, bool readable)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.isReadable = readable;
            importer.SaveAndReimport();
        }
    }

    private static void ProcessTextureCleanUp()
    {
        byte[] bytes = File.ReadAllBytes(INPUT_PATH);
        Texture2D originalTex = new Texture2D(2, 2);
        originalTex.LoadImage(bytes);

        int width = originalTex.width;
        int height = originalTex.height;

        Texture2D cleanTex = new Texture2D(width, height, TextureFormat.RGBA32, false);

        // Ambil warna latar dari pojok kiri atas sebagai acuan warna kunci (Cyan)
        Color bgColor = originalTex.GetPixel(5, height - 5);

        Color[] pixels = originalTex.GetPixels();
        Color[] cleanPixels = new Color[pixels.Length];

        int rowHeight = height / 5;
        int sliceWidth6 = width / 6; // Lebar sel untuk baris dengan 6 kolom
        int sliceWidth7 = width / 7; // Lebar sel untuk baris TURN (7 kolom)

        for (int y = 0; y < height; y++)
        {
            int row = y / rowHeight; // Baris 0 (bawah) sampai 4 (atas)

            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                Color pixelColor = pixels[index];

                // 1. TENTUKAN APAKAH PIKSEL ADALAH BAGIAN DARI TULISAN ATAU GARIS PEMBATAS
                bool isDividingLine = false;
                
                // Cek garis horizontal hitam pembatas baris (dengan ketebalan 8 piksel di sekitar perbatasan baris)
                for (int r = 1; r <= 4; r++)
                {
                    if (y >= r * rowHeight - 4 && y <= r * rowHeight + 4)
                    {
                        isDividingLine = true;
                        break;
                    }
                }

                bool isTextLabel = false;
                // Hapus tulisan teks di pojok kiri atas setiap baris
                // Kita kosongkan area pojok kiri atas pada setiap baris (lebar ~140 piksel, tinggi ~35 piksel dari atas baris)
                int localY = y % rowHeight;
                int cellWidth = (row == 0) ? sliceWidth7 : sliceWidth6; // TURN memiliki lebar kolom yang berbeda
                
                if (x < cellWidth * 0.95f && localY > rowHeight * 0.65f)
                {
                    // Hanya hapus jika piksel berwarna gelap (tulisan teks hitam)
                    // Atau jika piksel adalah bagian dari background cyan di sekitarnya
                    isTextLabel = true;
                }

                // 2. PROSES TRANSPARANSI BACKROUND CYAN
                float colorDistance = Mathf.Sqrt(
                    Mathf.Pow(pixelColor.r - bgColor.r, 2) +
                    Mathf.Pow(pixelColor.g - bgColor.g, 2) +
                    Mathf.Pow(pixelColor.b - bgColor.b, 2)
                );

                if (isDividingLine || isTextLabel || colorDistance < 0.2f)
                {
                    // Jadikan transparan
                    cleanPixels[index] = Color.clear;
                }
                else
                {
                    // Tetapkan warna asli (karakter fox)
                    cleanPixels[index] = pixelColor;
                }
            }
        }

        cleanTex.SetPixels(cleanPixels);
        cleanTex.Apply();

        byte[] outputBytes = cleanTex.EncodeToPNG();
        File.WriteAllBytes(OUTPUT_PATH, outputBytes);
    }

    private static void SliceCleanSpriteSheet()
    {
        // Tunggu Unity mengimpor file PNG transparan baru
        AssetDatabase.ImportAsset(OUTPUT_PATH);
        
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(OUTPUT_PATH);
        int width = tex.width;
        int height = tex.height;

        TextureImporter importer = AssetImporter.GetAtPath(OUTPUT_PATH) as TextureImporter;
        if (importer != null)
        {
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.filterMode = FilterMode.Point;
            importer.spritePixelsPerUnit = 16;

            List<SpriteMetaData> metas = new List<SpriteMetaData>();

            int rowHeight = height / 5;
            int colWidth6 = width / 6;
            int colWidth7 = width / 7;

            // Baris 4 (IDLE - 3 Frame, Kolom 0 s.d 2)
            for (int col = 0; col < 3; col++)
            {
                SpriteMetaData meta = new SpriteMetaData
                {
                    rect = new Rect(col * colWidth6, 4 * rowHeight, colWidth6, rowHeight),
                    name = "fox_idle_" + col,
                    alignment = (int)SpriteAlignment.Center
                };
                metas.Add(meta);
            }

            // Baris 3 (WALK - 6 Frame, Kolom 0 s.d 5)
            for (int col = 0; col < 6; col++)
            {
                SpriteMetaData meta = new SpriteMetaData
                {
                    rect = new Rect(col * colWidth6, 3 * rowHeight, colWidth6, rowHeight),
                    name = "fox_walk_" + col,
                    alignment = (int)SpriteAlignment.Center
                };
                metas.Add(meta);
            }

            // Baris 2 (JUMP - 4 Frame, Kolom 0 s.d 3)
            for (int col = 0; col < 4; col++)
            {
                SpriteMetaData meta = new SpriteMetaData
                {
                    rect = new Rect(col * colWidth6, 2 * rowHeight, colWidth6, rowHeight),
                    name = "fox_jump_" + col,
                    alignment = (int)SpriteAlignment.Center
                };
                metas.Add(meta);
            }

            // Baris 1 (DOUBLE JUMP - 3 Frame, Kolom 0 s.d 2)
            for (int col = 0; col < 3; col++)
            {
                SpriteMetaData meta = new SpriteMetaData
                {
                    rect = new Rect(col * colWidth6, 1 * rowHeight, colWidth6, rowHeight),
                    name = "fox_djump_" + col,
                    alignment = (int)SpriteAlignment.Center
                };
                metas.Add(meta);
            }

            // Baris 0 (TURN - 7 Frame, Kolom 0 s.d 6)
            for (int col = 0; col < 7; col++)
            {
                SpriteMetaData meta = new SpriteMetaData
                {
                    rect = new Rect(col * colWidth7, 0 * rowHeight, colWidth7, rowHeight),
                    name = "fox_turn_" + col,
                    alignment = (int)SpriteAlignment.Center
                };
                metas.Add(meta);
            }

            importer.spritesheet = metas.ToArray();
            importer.SaveAndReimport();
        }
    }

    private static AnimationClip CreateAnimationClip(List<Sprite> sprites, string clipName, bool loop, int fps)
    {
        AnimationClip clip = new AnimationClip();
        clip.frameRate = fps;

        if (loop)
        {
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
        }

        EditorCurveBinding spriteBinding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };

        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Count + 1];
        for (int i = 0; i < sprites.Count; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = i / (float)fps,
                value = sprites[i]
            };
        }
        // Loop frame penutup
        keyframes[sprites.Count] = new ObjectReferenceKeyframe
        {
            time = sprites.Count / (float)fps,
            value = sprites[sprites.Count - 1]
        };

        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyframes);

        string savePath = "Assets/aset/" + clipName + ".anim";
        AssetDatabase.CreateAsset(clip, savePath);
        return clip;
    }

    private static AnimatorController CreateAnimatorController(AnimationClip idleClip, AnimationClip walkClip, AnimationClip jumpClip, AnimationClip doubleJumpClip)
    {
        string controllerPath = "Assets/aset/Player_Controller.controller";

        if (File.Exists(controllerPath))
        {
            AssetDatabase.DeleteAsset(controllerPath);
        }

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

        // Tambahkan Parameters
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("isGrounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("doubleJump", AnimatorControllerParameterType.Trigger);

        // Buat States
        AnimatorState idleState = stateMachine.AddState("Idle");
        idleState.motion = idleClip;

        AnimatorState walkState = stateMachine.AddState("Walk");
        walkState.motion = walkClip;

        AnimatorState jumpState = stateMachine.AddState("Jump");
        jumpState.motion = jumpClip;

        AnimatorState doubleJumpState = stateMachine.AddState("DoubleJump");
        doubleJumpState.motion = doubleJumpClip;

        stateMachine.defaultState = idleState;

        // --- TRANSISI ---

        // Idle -> Walk
        AnimatorStateTransition toWalk = idleState.AddTransition(walkState);
        toWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        toWalk.hasExitTime = false;
        toWalk.duration = 0f;

        // Walk -> Idle
        AnimatorStateTransition toIdle = walkState.AddTransition(idleState);
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        toIdle.hasExitTime = false;
        toIdle.duration = 0f;

        // AnyState -> Jump
        AnimatorStateTransition anyToJump = stateMachine.AddAnyStateTransition(jumpState);
        anyToJump.AddCondition(AnimatorConditionMode.If, 0f, "isGrounded"); // isGrounded == false (If dalam Bool berarti true, tapi di mode condition: If = True, IfNot = False)
        // Tunggu, dalam AnimatorConditionMode: 
        // - Mode: If (arti: parameter bool bernilai true)
        // - Mode: IfNot (arti: parameter bool bernilai false)
        // Karena kita ingin pindah ke Jump saat isGrounded == false, maka gunakan IfNot!
        anyToJump.conditions = new AnimatorCondition[] {
            new AnimatorCondition { mode = AnimatorConditionMode.IfNot, parameter = "isGrounded", threshold = 0f }
        };
        anyToJump.hasExitTime = false;
        anyToJump.duration = 0f;

        // Jump -> DoubleJump (melalui Trigger)
        AnimatorStateTransition jumpToDjump = jumpState.AddTransition(doubleJumpState);
        jumpToDjump.AddCondition(AnimatorConditionMode.If, 0f, "doubleJump");
        jumpToDjump.hasExitTime = false;
        jumpToDjump.duration = 0f;

        // Jump -> Idle (Mendarat)
        AnimatorStateTransition jumpToIdle = jumpState.AddTransition(idleState);
        jumpToIdle.AddCondition(AnimatorConditionMode.If, 0f, "isGrounded"); // isGrounded == true
        jumpToIdle.hasExitTime = false;
        jumpToIdle.duration = 0f;

        // DoubleJump -> Idle (Mendarat)
        AnimatorStateTransition djumpToIdle = doubleJumpState.AddTransition(idleState);
        djumpToIdle.AddCondition(AnimatorConditionMode.If, 0f, "isGrounded"); // isGrounded == true
        djumpToIdle.hasExitTime = false;
        djumpToIdle.duration = 0f;

        AssetDatabase.SaveAssets();
        return controller;
    }
}
