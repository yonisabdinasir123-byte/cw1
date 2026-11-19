using System.Collections.Generic;
using Photon.Pun;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the Player and Projectile prefabs into Assets/Resources with every component and
/// reference wired, so PhotonNetwork.Instantiate can find them by name.
/// Safe to run again: existing prefabs are overwritten.
/// </summary>
public static class ArenaPrefabBuilder
{
    [MenuItem("Tools/Arena/2. Build Prefabs")]
    public static void BuildAll()
    {
        ArenaEditorUtil.EnsureTagsAndLayers();
        ArenaEditorUtil.EnsureFolder(ArenaEditorUtil.ResourcesFolder);
        BuildPlayerPrefab();
        BuildProjectilePrefab();
        BuildPowerUpPrefab();
        BuildEffectPrefab(NetworkKeys.HitEffectPrefab, new Color(1f, 0.6f, 0.15f), 14, 0.2f, 5f, 0.35f);
        BuildEffectPrefab(NetworkKeys.DeathEffectPrefab, new Color(1f, 0.25f, 0.2f), 60, 0.45f, 9f, 0.9f);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Arena: Player and Projectile prefabs built in " + ArenaEditorUtil.ResourcesFolder);
    }

    private static void BuildPlayerPrefab()
    {
        Material bodyMaterial = ArenaEditorUtil.CreateMaterial("PlayerBody", new Color(0.25f, 0.55f, 1f));
        Material noseMaterial = ArenaEditorUtil.CreateMaterial("PlayerNose", new Color(1f, 0.85f, 0.2f));

        GameObject root = new GameObject(NetworkKeys.PlayerPrefab);
        root.tag = GameTags.PlayerTag;
        root.layer = LayerMask.NameToLayer(GameTags.PlayerLayer);

        // Visual body: a capsule standing on the floor (centre at y = 1).
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = new Vector3(0f, 1f, 0f);
        body.layer = root.layer;
        Object.DestroyImmediate(body.GetComponent<Collider>());
        body.GetComponent<Renderer>().sharedMaterial = bodyMaterial;

        // Nose: shows which way the player is facing and therefore where shots go.
        GameObject nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
        nose.name = "Nose";
        nose.transform.SetParent(root.transform, false);
        nose.transform.localPosition = new Vector3(0f, 1f, 0.55f);
        nose.transform.localScale = new Vector3(0.3f, 0.3f, 0.5f);
        nose.layer = root.layer;
        Object.DestroyImmediate(nose.GetComponent<Collider>());
        nose.GetComponent<Renderer>().sharedMaterial = noseMaterial;

        // Muzzle: projectile spawn point just outside the collider (radius 0.5).
        GameObject muzzle = new GameObject("Muzzle");
        muzzle.transform.SetParent(root.transform, false);
        muzzle.transform.localPosition = new Vector3(0f, 1f, 1.1f);

        // Name tag above the head.
        GameObject tag = new GameObject("NameTag");
        tag.transform.SetParent(root.transform, false);
        tag.transform.localPosition = new Vector3(0f, 2.6f, 0f);
        TextMesh textMesh = tag.AddComponent<TextMesh>();
        textMesh.font = ArenaEditorUtil.DefaultFont;
        textMesh.fontSize = 48;
        textMesh.characterSize = 0.12f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.text = "Player";
        tag.GetComponent<MeshRenderer>().sharedMaterial = ArenaEditorUtil.DefaultFont.material;

        // Physics on the root so walls block movement.
        CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
        collider.center = new Vector3(0f, 1f, 0f);
        collider.height = 2f;
        collider.radius = 0.5f;

        Rigidbody rigidbody = root.AddComponent<Rigidbody>();
        rigidbody.mass = 1f;
        rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
        rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Networking: PhotonView observes the transform view (position, rotation) and health.
        PhotonView view = root.AddComponent<PhotonView>();
        PhotonTransformView transformView = root.AddComponent<PhotonTransformView>();
        transformView.m_SynchronizePosition = true;
        transformView.m_SynchronizeRotation = true;
        transformView.m_SynchronizeScale = false;

        // Shield bubble, hidden until a shield power-up is active.
        Material shieldMaterial = ArenaEditorUtil.CreateUnlitMaterial("Shield", new Color(0.6f, 0.5f, 1f, 0.35f));
        GameObject shield = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shield.name = "ShieldVisual";
        shield.transform.SetParent(root.transform, false);
        shield.transform.localPosition = new Vector3(0f, 1f, 0f);
        shield.transform.localScale = Vector3.one * 2.4f;
        shield.layer = root.layer;
        Object.DestroyImmediate(shield.GetComponent<Collider>());
        shield.GetComponent<Renderer>().sharedMaterial = shieldMaterial;
        shield.SetActive(false);

        // Dash trail.
        Material trailMaterial = ArenaEditorUtil.CreateUnlitMaterial("DashTrail", new Color(0.4f, 0.9f, 1f, 0.8f));
        GameObject trailObject = new GameObject("DashTrail");
        trailObject.transform.SetParent(root.transform, false);
        trailObject.transform.localPosition = new Vector3(0f, 1f, 0f);
        TrailRenderer trail = trailObject.AddComponent<TrailRenderer>();
        trail.sharedMaterial = trailMaterial;
        trail.time = 0.25f;
        trail.startWidth = 0.8f;
        trail.endWidth = 0.1f;
        trail.minVertexDistance = 0.05f;
        trail.emitting = false;

        PlayerMovement movement = root.AddComponent<PlayerMovement>();
        PlayerShooting shooting = root.AddComponent<PlayerShooting>();
        PlayerHealth health = root.AddComponent<PlayerHealth>();
        PlayerNameTag nameTag = root.AddComponent<PlayerNameTag>();
        PlayerPowerUps powerUps = root.AddComponent<PlayerPowerUps>();

        ArenaEditorUtil.SetReference(shooting, "muzzle", muzzle.transform);
        ArenaEditorUtil.SetReference(nameTag, "label", textMesh);
        ArenaEditorUtil.SetReference(movement, "dashTrail", trail);
        ArenaEditorUtil.SetReference(powerUps, "shieldVisual", shield);

        view.ObservedComponents = new List<Component> { transformView, health };
        view.Synchronization = ViewSynchronization.UnreliableOnChange;
        view.OwnershipTransfer = OwnershipOption.Fixed;

        SavePrefab(root, NetworkKeys.PlayerPrefab);
    }

    private static void BuildProjectilePrefab()
    {
        Material material = ArenaEditorUtil.CreateMaterial("Projectile", new Color(1f, 0.45f, 0.1f));

        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        root.name = NetworkKeys.ProjectilePrefab;
        root.tag = GameTags.ProjectileTag;
        root.layer = LayerMask.NameToLayer(GameTags.ProjectileLayer);
        root.transform.localScale = Vector3.one * 0.4f;
        root.GetComponent<Renderer>().sharedMaterial = material;

        // Trigger collider: we only want the hit event, not a physical bounce.
        SphereCollider collider = root.GetComponent<SphereCollider>();
        collider.isTrigger = true;

        Rigidbody rigidbody = root.AddComponent<Rigidbody>();
        rigidbody.isKinematic = true;
        rigidbody.useGravity = false;
        rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        // The projectile is deterministic, so the view only provides ownership and a network ID.
        PhotonView view = root.AddComponent<PhotonView>();
        view.ObservedComponents = new List<Component>();
        view.Synchronization = ViewSynchronization.Off;
        view.OwnershipTransfer = OwnershipOption.Fixed;

        root.AddComponent<Projectile>();

        SavePrefab(root, NetworkKeys.ProjectilePrefab);
    }

    private static void BuildPowerUpPrefab()
    {
        Material material = ArenaEditorUtil.CreateMaterial("PowerUp", Color.white);

        GameObject root = new GameObject(NetworkKeys.PowerUpPrefab);
        BoxCollider trigger = root.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.center = new Vector3(0f, 1f, 0f);
        trigger.size = new Vector3(1.4f, 2f, 1.4f);

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "Visual";
        visual.transform.SetParent(root.transform, false);
        visual.transform.localPosition = new Vector3(0f, 1f, 0f);
        visual.transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
        visual.transform.localScale = Vector3.one * 0.6f;
        Object.DestroyImmediate(visual.GetComponent<Collider>());
        visual.GetComponent<Renderer>().sharedMaterial = material;

        PhotonView view = root.AddComponent<PhotonView>();
        view.ObservedComponents = new List<Component>();
        view.Synchronization = ViewSynchronization.Off;
        view.OwnershipTransfer = OwnershipOption.Fixed;

        PowerUp powerUp = root.AddComponent<PowerUp>();
        ArenaEditorUtil.SetReference(powerUp, "visual", visual.GetComponent<Renderer>());

        SavePrefab(root, NetworkKeys.PowerUpPrefab);
    }

    /// <summary>A one-shot particle burst that destroys itself when finished. Local only, not networked.</summary>
    private static void BuildEffectPrefab(string name, Color color, int particleCount, float lifetime, float speed, float size)
    {
        Material material = ArenaEditorUtil.CreateUnlitMaterial("Particle", Color.white);

        GameObject root = new GameObject(name);
        ParticleSystem system = root.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = system.main;
        main.duration = lifetime;
        main.loop = false;
        main.playOnAwake = true;
        main.startLifetime = lifetime;
        main.startSpeed = speed;
        main.startSize = size;
        main.startColor = color;
        main.gravityModifier = 0.5f;
        main.stopAction = ParticleSystemStopAction.Destroy;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = system.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)particleCount) });

        ParticleSystem.ShapeModule shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.15f;

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = system.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        ParticleSystemRenderer renderer = root.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;

        SavePrefab(root, name);
    }

    private static void SavePrefab(GameObject root, string name)
    {
        string path = ArenaEditorUtil.ResourcesFolder + "/" + name + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }
}
