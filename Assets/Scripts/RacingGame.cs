using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class RacingGame : MonoBehaviour
{
    private Transform car;
    private Transform cameraPivot;
    private float speed;
    private float steer;
    private bool gas, brake, left, right;
    private Text speedText;

    void Start()
    {
        Application.targetFrameRate = 60;
        BuildWorld();
        BuildCar();
        BuildCamera();
        BuildUI();
    }

    void Update()
    {
        float forward = Input.GetAxisRaw("Vertical");
        float turn = Input.GetAxisRaw("Horizontal");
        if (gas) forward = 1f;
        if (brake) forward = -0.65f;
        if (left) turn = -1f;
        if (right) turn = 1f;

        float target = forward >= 0 ? forward * 22f : forward * 9f;
        speed = Mathf.MoveTowards(speed, target, 18f * Time.deltaTime);
        steer = Mathf.MoveTowards(steer, turn, 3f * Time.deltaTime);

        if (car != null)
        {
            car.Translate(Vector3.forward * speed * Time.deltaTime);
            if (Mathf.Abs(speed) > 0.3f)
                car.Rotate(Vector3.up, steer * Mathf.Sign(speed) * 85f * Time.deltaTime);
        }

        if (speedText != null)
            speedText.text = Mathf.RoundToInt(Mathf.Abs(speed) * 3.6f) + " км/ч";
    }

    void LateUpdate()
    {
        if (car == null || cameraPivot == null) return;
        Vector3 desired = car.position - car.forward * 7f + Vector3.up * 4f;
        Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, desired, 5f * Time.deltaTime);
        Camera.main.transform.LookAt(car.position + Vector3.up * 1.1f);
    }

    void BuildWorld()
    {
        RenderSettings.ambientLight = new Color(0.65f, 0.68f, 0.72f);
        CreatePrimitive(PrimitiveType.Plane, "Ground", new Vector3(0,-0.12f,0), new Vector3(12,1,12), new Color(0.22f,0.48f,0.22f));
        CreatePrimitive(PrimitiveType.Cube, "Road", new Vector3(0,0,15), new Vector3(8,0.08f,100), new Color(0.12f,0.12f,0.13f));
        for (int z = -25; z < 65; z += 10)
        {
            CreatePrimitive(PrimitiveType.Cube, "Road marking", new Vector3(0,0.06f,z), new Vector3(0.18f,0.02f,4), Color.white);
        }
        CreatePrimitive(PrimitiveType.Cube, "Left barrier", new Vector3(-4.2f,0.35f,15), new Vector3(0.25f,0.7f,100), Color.yellow);
        CreatePrimitive(PrimitiveType.Cube, "Right barrier", new Vector3(4.2f,0.35f,15), new Vector3(0.25f,0.7f,100), Color.yellow);

        Light sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.15f;
        sun.transform.rotation = Quaternion.Euler(50,-30,0);
    }

    void BuildCar()
    {
        car = new GameObject("T2X2 Player Car").transform;
        car.position = new Vector3(0,0.55f,-5);
        MakePart("Body", car, PrimitiveType.Cube, new Vector3(0,0.45f,0), new Vector3(1.8f,0.55f,3.4f), new Color(0.05f,0.35f,0.95f));
        MakePart("Cabin", car, PrimitiveType.Cube, new Vector3(0,0.95f,-0.25f), new Vector3(1.35f,0.65f,1.55f), new Color(0.12f,0.18f,0.24f));
        for (int side = -1; side <= 1; side += 2)
        for (int z = -1; z <= 1; z += 2)
        {
            MakePart("Wheel", car, PrimitiveType.Cylinder, new Vector3(side*0.98f,0.15f,z*1.05f), new Vector3(0.28f,0.16f,0.28f), Color.black).localRotation = Quaternion.Euler(0,0,90);
        }
    }

    void BuildCamera()
    {
        if (Camera.main == null)
        {
            GameObject cam = new GameObject("Main Camera");
            cam.tag = "MainCamera";
            cam.AddComponent<Camera>();
        }
        Camera.main.transform.position = new Vector3(0,4,-12);
        Camera.main.fieldOfView = 65;
        cameraPivot = car;
    }

    void BuildUI()
    {
        GameObject canvasObject = new GameObject("Mobile HUD");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.AddComponent<GraphicRaycaster>();
        if (FindObjectOfType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        speedText = MakeText(canvas.transform, "0 км/ч", new Vector2(0.82f,0.88f), new Vector2(220,70), 30, TextAnchor.MiddleCenter);

        MakeHoldButton(canvas.transform, "◀", new Vector2(0.12f,0.18f), () => left=true, () => left=false);
        MakeHoldButton(canvas.transform, "▶", new Vector2(0.28f,0.18f), () => right=true, () => right=false);
        MakeHoldButton(canvas.transform, "ГАЗ", new Vector2(0.86f,0.2f), () => gas=true, () => gas=false);
        MakeHoldButton(canvas.transform, "ТОРМОЗ", new Vector2(0.70f,0.2f), () => brake=true, () => brake=false);
    }

    Text MakeText(Transform parent, string value, Vector2 anchor, Vector2 size, int fontSize, TextAnchor align)
    {
        GameObject go = new GameObject("Speedometer");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;
        Text text = go.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = align;
        text.color = Color.white;
        return text;
    }

    void MakeHoldButton(Transform parent, string label, Vector2 anchor, System.Action down, System.Action up)
    {
        GameObject go = new GameObject(label);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(125,90);
        Image image = go.AddComponent<Image>();
        image.color = new Color(0.05f,0.08f,0.12f,0.75f);
        Button button = go.AddComponent<Button>();
        Text text = MakeText(go.transform, label, new Vector2(0.5f,0.5f), new Vector2(120,80), 22, TextAnchor.MiddleCenter);
        text.rectTransform.anchoredPosition = Vector2.zero;
        EventTrigger trigger = go.AddComponent<EventTrigger>();
        AddTrigger(trigger, EventTriggerType.PointerDown, down);
        AddTrigger(trigger, EventTriggerType.PointerUp, up);
        AddTrigger(trigger, EventTriggerType.PointerExit, up);
    }

    void AddTrigger(EventTrigger trigger, EventTriggerType type, System.Action action)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener((data) => action());
        trigger.triggers.Add(entry);
    }

    GameObject CreatePrimitive(PrimitiveType type, string name, Vector3 pos, Vector3 scale, Color color)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.position = pos;
        obj.transform.localScale = scale;
        var renderer = obj.GetComponent<Renderer>();
        renderer.material = new Material(Shader.Find("Standard"));
        renderer.material.color = color;
        return obj;
    }

    Transform MakePart(string name, Transform parent, PrimitiveType type, Vector3 localPos, Vector3 scale, Color color)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = localPos;
        obj.transform.localScale = scale;
        var renderer = obj.GetComponent<Renderer>();
        renderer.material = new Material(Shader.Find("Standard"));
        renderer.material.color = color;
        Destroy(obj.GetComponent<Collider>());
        return obj.transform;
    }
}
