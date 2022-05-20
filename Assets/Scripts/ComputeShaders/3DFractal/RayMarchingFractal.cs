
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class RayMarchingFractal : MonoBehaviour
{
    [SerializeField]
    private ComputeShader rayMarchingFractalShader;
    
    public FractalType fractalType;

    [Range(1, 20), SerializeField] 
    private float fractalPower;
    [Range(0.000001f, 0.1f), SerializeField] 
    private float epsilon = 0.001f;
    [SerializeField]

    public enum FractalType
    {
        MadelBulb,
        TetrahedronFractal,
        Spheres,
        InfiniteTetrahedronFractal,
    }
    

    [SerializeField, Range(0, 300)] private float fractalPowerMax = 40;

    [SerializeField]
    private float darkness = 70;

    [Range(0, 1), SerializeField] private float blackAndWhite;
    [Range(0, 1), SerializeField] private float redA;
    [Range(0, 1), SerializeField] private float greenA;
    [Range(0, 1), SerializeField] private float blueA;
    [Range(0, 1), SerializeField] private float redB;
    [Range(0, 1), SerializeField] private float greenB;
    [Range(0, 1), SerializeField] private float blueB;

    private RenderTexture target;
    private Camera cam;
    private Light directionalLight;

    [FormerlySerializedAs("CameraSpeed")]
    [Header("Camera")]
    [Range(0, 100), SerializeField] private float cameraSpeedMax = 10;
    [Range(1, 30), SerializeField] private float mouseSensitivity = 10;

    private float cameraSpeed;

    [Header("Animation Options"), SerializeField]
    private float powerIncreaseSpeed = 0.2f;
    [SerializeField] private bool isAnimated = false;
    [SerializeField, Range(-20,0)] private float powerIncreaseMin = -2;
    [SerializeField, Range(0, 20)] private float powerIncreaseMax = 2;
    [SerializeField, Range(0, 2)] private float powerIncreaseSpeedSensitivity = 0.2f;

    [Header("UIEvents")]
    [SerializeField] private UnityEvent<float> powerIncreaseSpeedChanged = new UnityEvent<float>();
    [SerializeField] private UnityEvent<float> powerChanged = new UnityEvent<float>();

    private bool isMouseLocked = true;
    private bool allowMouseInput = true;
    private void Start()
    {
        cameraSpeed = 1;
        Application.targetFrameRate = 60;
        powerIncreaseSpeedChanged.Invoke(powerIncreaseSpeed);
        powerChanged.Invoke(fractalPower);
        SetMouseLockedState(true);
    }

    private void Init()
    {
        cam = Camera.current;
        directionalLight = FindObjectOfType<Light>();
    }

    private void Update()
    {
        if (Application.isPlaying && isAnimated)
        {
            fractalPower += powerIncreaseSpeed * Time.deltaTime;
            fractalPower = Mathf.Clamp(fractalPower, 1, fractalPowerMax);
            powerChanged.Invoke(fractalPower);
        }

        if(!cam) return;
        if (Input.GetAxis("Vertical") != 0 )
        {
                cam.transform.position +=
                    cam.transform.forward * Input.GetAxis("Vertical") * cameraSpeed * Time.deltaTime;
        }
        if (Input.GetAxis("Horizontal") != 0)
        {
                cam.transform.position +=
                    cam.transform.right * Input.GetAxis("Horizontal") * cameraSpeed * Time.deltaTime;
        }
        if (Input.GetAxis("Mouse X") != 0 && allowMouseInput)
        {
                cam.transform.rotation *=
                    Quaternion.Euler(0, Input.GetAxis("Mouse X") * Time.deltaTime * mouseSensitivity, 0);
        }
        if (Input.GetAxis("Mouse Y") != 0 && allowMouseInput)
        {
                cam.transform.rotation *=
                    Quaternion.Euler(-Input.GetAxis("Mouse Y") * Time.deltaTime * mouseSensitivity, 0, 0);
        }

        if (Input.GetAxis("Mouse ScrollWheel") != 0)
        {
            cameraSpeed =
                Mathf.Clamp(cameraSpeed + Input.GetAxis("Mouse ScrollWheel") * Time.deltaTime * cameraSpeed * 6,
                    0, cameraSpeedMax);
            Debug.Log(cameraSpeed);
        }

        if (Input.GetAxis("RotateHorizontal") != 0)
        {
            cam.transform.rotation *=
                Quaternion.Euler(0, Input.GetAxis("RotateHorizontal") * Time.deltaTime * mouseSensitivity, 0);

        }
        
        if (Input.GetAxis("RotateVertical") != 0)
        {
            cam.transform.rotation *=
                Quaternion.Euler(-Input.GetAxis("RotateVertical") * Time.deltaTime * mouseSensitivity, 0, 0);
        }


        if (Input.GetButtonDown("Pause"))
        {
            powerIncreaseSpeed = 0;
            powerIncreaseSpeedChanged.Invoke(0);
        }

        if (Input.GetButton("FastForward"))
        {
            if (fractalPower.Equals(1)) powerIncreaseSpeed = 0;
            powerIncreaseSpeed += Time.deltaTime * powerIncreaseSpeedSensitivity;
            powerIncreaseSpeed = Mathf.Min(powerIncreaseMax, powerIncreaseSpeed);
            powerIncreaseSpeedChanged.Invoke(powerIncreaseSpeed);
        }
        if (Input.GetButton("SlowDown"))
        {
            if (fractalPowerMax.Equals(fractalPower)) powerIncreaseSpeed = 0;
            powerIncreaseSpeed -= Time.deltaTime * powerIncreaseSpeedSensitivity;
            powerIncreaseSpeed = Mathf.Max(powerIncreaseMin, powerIncreaseSpeed);
            powerIncreaseSpeedChanged.Invoke(powerIncreaseSpeed);
        }

        if (Input.GetAxis("ChangeDetailAmount") != 0)
        {
            epsilon = Mathf.Clamp(epsilon + Input.GetAxis("ChangeDetailAmount") * epsilon * Time.deltaTime,
                .000001f, .15f);
        }

        if (Input.GetButtonDown("ToggleMouseLocked"))
        {
            ToggleMouseState();
        }
    }

    private void ToggleMouseState()
    {
        isMouseLocked = !isMouseLocked;
        SetMouseLockedState(isMouseLocked);
    }

    private void SetMouseLockedState(bool state)
    {
        allowMouseInput = state;
        Cursor.visible = !state;
        Cursor.lockState = state ? CursorLockMode.Locked : CursorLockMode.Confined;
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        Init();
        InitRenderTexture();
        SetParameters();

        int threadGroupsX = Mathf.CeilToInt(cam.pixelWidth / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(cam.pixelHeight / 8.0f);
        rayMarchingFractalShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);
        
        Graphics.Blit(target, dest);
    }

    private void SetParameters()
    {
        rayMarchingFractalShader.SetTexture(0, "Destination", target);
        
        rayMarchingFractalShader.SetInt("type", (int)fractalType);
        rayMarchingFractalShader.SetFloat("power", Mathf.Max(fractalPower, 1,01));
        rayMarchingFractalShader.SetFloat("epsilon", epsilon);
        rayMarchingFractalShader.SetFloat("darkness", darkness);
        rayMarchingFractalShader.SetFloat("blackAndWhite", blackAndWhite);
        
        rayMarchingFractalShader.SetVector("colorAMix", new Vector3(redA, greenA, blueA));
        rayMarchingFractalShader.SetVector("colorBMix", new Vector3(redB, greenB, blueB));
        
        rayMarchingFractalShader.SetMatrix("_CameraToWorld", cam.cameraToWorldMatrix);
        rayMarchingFractalShader.SetMatrix("_CameraInverseProjection", cam.projectionMatrix.inverse);
        rayMarchingFractalShader.SetVector("_LightDirection", directionalLight.transform.forward);
        
    }

    private void InitRenderTexture()
    {
        if (!target || target.width != cam.scaledPixelWidth || target.height != cam.pixelHeight)
        {
            if (target)
            {
                target.Release();
            }

            target = new RenderTexture(cam.pixelWidth, cam.pixelHeight, 0, RenderTextureFormat.ARGBFloat,
                RenderTextureReadWrite.Linear);
            target.enableRandomWrite = true;
            target.Create();
        }

    }
}
