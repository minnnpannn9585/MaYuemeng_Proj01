using UnityEngine;

public class FlashlightController : MonoBehaviour
{
    [SerializeField] private Transform followTarget;
    [SerializeField] private float range = 6f;
    [SerializeField] private float coneAngle = 55f;
    [SerializeField] private int meshSegments = 24;
    [SerializeField] private Color lightColor = new Color(1f, 0.92f, 0.55f, 0.4f);

    private Camera mainCamera;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh mesh;
    private Vector3[] vertices;
    private Color[] colors;
    private int[] triangles;

    public bool IsOn { get; private set; }
    public Vector2 Origin { get; private set; }
    public Vector2 AimDirection { get; private set; }
    public float Range => range;
    public float ConeAngle => coneAngle;

    private void Awake()
    {
        mainCamera = Camera.main;
        ResolveFollowTarget();
        CreateBeam();
    }

    private void Update()
    {
        ResolveFollowTarget();

        Origin = followTarget != null ? followTarget.position : transform.position;
        IsOn = Input.GetMouseButton(0);
        AimDirection = GetMouseAimDirection();

        transform.position = Origin;
        float angleZ = Mathf.Atan2(AimDirection.y, AimDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angleZ);

        if (meshRenderer != null)
        {
            meshRenderer.enabled = IsOn;
        }
    }

    public bool Contains(Vector2 worldPoint)
    {
        if (!IsOn)
        {
            return false;
        }

        Vector2 toPoint = worldPoint - Origin;
        float distance = toPoint.magnitude;
        if (distance > range)
        {
            return false;
        }

        if (distance < 0.001f)
        {
            return true;
        }

        return Vector2.Angle(AimDirection, toPoint) <= coneAngle * 0.5f;
    }

    private void ResolveFollowTarget()
    {
        if (followTarget != null)
        {
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            followTarget = player.transform;
        }
    }

    private Vector2 GetMouseAimDirection()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            return Vector2.right;
        }

        Vector3 mouseScreen = Input.mousePosition;
        mouseScreen.z = Mathf.Abs(mainCamera.transform.position.z);
        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(mouseScreen);
        Vector2 direction = (Vector2)mouseWorld - Origin;

        if (direction.sqrMagnitude < 0.0001f)
        {
            return Vector2.right;
        }

        return direction.normalized;
    }

    private void CreateBeam()
    {
        GameObject beamObject = new GameObject("FlashlightBeam");
        beamObject.transform.SetParent(transform, false);
        beamObject.transform.localPosition = Vector3.zero;
        beamObject.transform.localRotation = Quaternion.identity;

        meshFilter = beamObject.AddComponent<MeshFilter>();
        meshRenderer = beamObject.AddComponent<MeshRenderer>();

        Shader beamShader = Shader.Find("Sprites/Default");
        if (beamShader == null)
        {
            beamShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        }

        if (beamShader != null)
        {
            Material beamMaterial = new Material(beamShader);
            beamMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            meshRenderer.material = beamMaterial;
        }
        meshRenderer.sortingOrder = 8;
        meshRenderer.enabled = false;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        mesh = new Mesh { name = "FlashlightCone" };
        meshFilter.mesh = mesh;
        BuildConeMesh();
    }

    private void BuildConeMesh()
    {
        int vertexCount = meshSegments + 2;
        vertices = new Vector3[vertexCount];
        colors = new Color[vertexCount];
        triangles = new int[meshSegments * 3];

        vertices[0] = Vector3.zero;
        colors[0] = lightColor;

        Color rimColor = new Color(lightColor.r, lightColor.g, lightColor.b, 0f);
        float halfAngle = coneAngle * 0.5f;

        for (int i = 0; i <= meshSegments; i++)
        {
            float t = i / (float)meshSegments;
            float angle = (-halfAngle + coneAngle * t) * Mathf.Deg2Rad;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * range;
            colors[i + 1] = rimColor;
        }

        for (int i = 0; i < meshSegments; i++)
        {
            int triangleIndex = i * 3;
            triangles[triangleIndex] = 0;
            triangles[triangleIndex + 1] = i + 1;
            triangles[triangleIndex + 2] = i + 2;
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
    }

    private void OnDestroy()
    {
        if (mesh != null)
        {
            Destroy(mesh);
        }

        if (meshRenderer != null && meshRenderer.material != null)
        {
            Destroy(meshRenderer.material);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = followTarget != null ? followTarget.position : transform.position;
        Vector3 direction = Application.isPlaying ? (Vector3)AimDirection : Vector3.right;
        float halfAngle = coneAngle * 0.5f;

        Gizmos.color = new Color(1f, 0.9f, 0.4f, 0.35f);
        Vector3 left = Quaternion.Euler(0f, 0f, halfAngle) * direction * range;
        Vector3 right = Quaternion.Euler(0f, 0f, -halfAngle) * direction * range;
        Gizmos.DrawLine(origin, origin + left);
        Gizmos.DrawLine(origin, origin + right);
        Gizmos.DrawLine(origin + left, origin + right);
    }
}
