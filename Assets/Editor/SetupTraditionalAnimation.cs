using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Collections.Generic;

public class SetupTraditionalAnimation : EditorWindow
{
    private const string SPRITE_SHEET_PATH = "Assets/aset/fox_sprite_sheet.png";

    [MenuItem("Cherry Rush/Setup Traditional Animation")]
    public static void SetupAnimation()
    {
        if (!File.Exists(SPRITE_SHEET_PATH))
        {
            Debug.LogError("Sprite sheet tidak ditemukan di " + SPRITE_SHEET_PATH + ". Pastikan file tersebut sudah ada.");
            return;
        }

        // 1. Buat texture readable untuk diproses transparansinya
        ConfigureTextureReadable(SPRITE_SHEET_PATH, true);

        // 2. Ubah background putih menjadi transparan secara prosedural
        ProcessTransparency(SPRITE_SHEET_PATH);

        // 3. Potong Sprite Sheet (6 kolom, 2 baris)
        SliceSpriteSheet(SPRITE_SHEET_PATH);

        // 4. Muat ulang Aset agar potongan sprite terbaca
        AssetDatabase.ImportAsset(SPRITE_SHEET_PATH);
        AssetDatabase.Refresh();

        // Ambil potongan sprite yang sudah dislice
        object[] assets = AssetDatabase.LoadAllAssetsAtPath(SPRITE_SHEET_PATH);
        List<Sprite> idleSprites = new List<Sprite>();
        List<Sprite> runSprites = new List<Sprite>();

        foreach (var asset in assets)
        {
            if (asset is Sprite sprite)
            {
                if (sprite.name.Contains("idle"))
                    idleSprites.Add(sprite);
                else if (sprite.name.Contains("run"))
                    runSprites.Add(sprite);
            }
        }

        // Urutkan sprite berdasarkan nama agar animasinya runtut
        idleSprites.Sort((a, b) => a.name.CompareTo(b.name));
        runSprites.Sort((a, b) => a.name.CompareTo(b.name));

        if (idleSprites.Count == 0 || runSprites.Count == 0)
        {
            Debug.LogError("Gagal memuat potongan sprite. Pastikan proses slicing berhasil.");
            return;
        }

        // 5. Buat File .anim (Animation Clip) secara programmatik
        AnimationClip idleClip = CreateAnimationClip(idleSprites, "Player_Idle", true);
        AnimationClip runClip = CreateAnimationClip(runSprites, "Player_Run", true);

        // 6. Buat Animator Controller
        AnimatorController animatorController = CreateAnimatorController(idleClip, runClip);

        // 7. Cari Player di Scene & Hubungkan Komponen
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj == null)
        {
            playerObj = GameObject.Find("player_sprite-removebg-preview_0");
        }

        if (playerObj != null)
        {
            // Matikan PlayerAnimator prosedural lama jika terpasang agar tidak konflik
            var oldProcAnim = playerObj.GetComponent<PlayerAnimator>();
            if (oldProcAnim != null)
            {
                DestroyImmediate(oldProcAnim);
            }

            // Tambahkan Animator jika belum ada
            Animator animator = playerObj.GetComponent<Animator>();
            if (animator == null)
            {
                animator = playerObj.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = animatorController;

            // Set sprite pertama sebagai tampilan default
            SpriteRenderer sr = playerObj.GetComponent<SpriteRenderer>();
            if (sr != null && idleSprites.Count > 0)
            {
                sr.sprite = idleSprites[0];
            }

            Debug.Log("Traditional Animation sukses dipasang ke " + playerObj.name + "! Klik Play untuk menguji.");
        }
        else
        {
            Debug.LogWarning("Karakter Player tidak ditemukan di scene saat ini. Animator Controller telah dibuat di Assets/aset/Player_Controller.controller. Silakan pasang secara manual ke karakter Anda.");
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

    private static void ProcessTransparency(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(bytes);

        Color[] pixels = tex.GetPixels();
        for (int i = 0; i < pixels.Length; i++)
        {
            Color c = pixels[i];
            // Ubah piksel putih/mendekati putih menjadi transparan (Alpha = 0)
            if (c.r > 0.92f && c.g > 0.92f && c.b > 0.92f)
            {
                pixels[i] = Color.clear;
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();

        byte[] outputBytes = tex.EncodeToPNG();
        File.WriteAllBytes(path, outputBytes);
    }

    private static void SliceSpriteSheet(string path)
    {
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        int texWidth = tex.width;
        int texHeight = tex.height;

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.filterMode = FilterMode.Point;
            importer.spritePixelsPerUnit = 16;

            List<SpriteMetaData> metas = new List<SpriteMetaData>();

            // 6 Kolom, 2 Baris
            int sliceWidth = texWidth / 6;
            int sliceHeight = texHeight / 2;

            // Baris Atas (Row 0 di Unity texture space adalah baris bawah, maka baris atas adalah Row 1)
            // Baris atas: 4 Frame Idle (kolom 0-3)
            for (int col = 0; col < 4; col++)
            {
                SpriteMetaData meta = new SpriteMetaData
                {
                    rect = new Rect(col * sliceWidth, 1 * sliceHeight, sliceWidth, sliceHeight),
                    name = "fox_idle_" + col,
                    alignment = (int)SpriteAlignment.Center
                };
                metas.Add(meta);
            }

            // Baris Bawah (Row 0 di Unity texture space)
            // Baris bawah: 6 Frame Lari (kolom 0-5)
            for (int col = 0; col < 6; col++)
            {
                SpriteMetaData meta = new SpriteMetaData
                {
                    rect = new Rect(col * sliceWidth, 0 * sliceHeight, sliceWidth, sliceHeight),
                    name = "fox_run_" + col,
                    alignment = (int)SpriteAlignment.Center
                };
                metas.Add(meta);
            }

            importer.spritesheet = metas.ToArray();
            importer.SaveAndReimport();
        }
    }

    private static AnimationClip CreateAnimationClip(List<Sprite> sprites, string clipName, bool loop)
    {
        AnimationClip clip = new AnimationClip();
        clip.frameRate = 10; // Kecepatan 10 FPS agar gerakannya pas

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
                time = i / 10f,
                value = sprites[i]
            };
        }
        // Frame terakhir berulang sebentar agar loop terasa halus
        keyframes[sprites.Count] = new ObjectReferenceKeyframe
        {
            time = sprites.Count / 10f,
            value = sprites[sprites.Count - 1]
        };

        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyframes);

        string savePath = "Assets/aset/" + clipName + ".anim";
        AssetDatabase.CreateAsset(clip, savePath);
        return clip;
    }

    private static AnimatorController CreateAnimatorController(AnimationClip idleClip, AnimationClip runClip)
    {
        string controllerPath = "Assets/aset/Player_Controller.controller";
        
        // Hapus file controller lama jika sudah ada agar terbuat yang bersih
        if (File.Exists(controllerPath))
        {
            AssetDatabase.DeleteAsset(controllerPath);
        }

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

        // Tambahkan Parameter untuk memicu transisi
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("isGrounded", AnimatorControllerParameterType.Bool);

        // Tambahkan State ke Animator
        AnimatorState idleState = stateMachine.AddState("Idle");
        idleState.motion = idleClip;

        AnimatorState runState = stateMachine.AddState("Run");
        runState.motion = runClip;

        stateMachine.defaultState = idleState;

        // Buat Transisi Idle -> Run (ketika berjalan)
        AnimatorStateTransition toRun = idleState.AddTransition(runState);
        toRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        toRun.hasExitTime = false;
        toRun.duration = 0f; // Instan tanpa blending/crossfade agar responsif (pixel-art)

        // Buat Transisi Run -> Idle (ketika berhenti)
        AnimatorStateTransition toIdle = runState.AddTransition(idleState);
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        toIdle.hasExitTime = false;
        toIdle.duration = 0f;

        AssetDatabase.SaveAssets();
        return controller;
    }
}
