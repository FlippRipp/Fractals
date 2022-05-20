using System;
using UnityEngine;
using UnityEngine.UI;

public class MandelbrotCS : MonoBehaviour
{
    [SerializeField]
    private double width, height;
    [SerializeField]
    private double rStart, iStart;
    [SerializeField]
    private int maxIterations;
    [SerializeField]
    private int increment;
    [SerializeField]
    private float zoom;

    //compute
    public ComputeShader shader;
    private ComputeBuffer buffer;
    private RenderTexture texture;
    public RawImage image;

    //Data for the compute buffer
    public struct Data
    {
        public double w, h, r, i;
        public int screenHeight, screenWidth;
    }

    private Data[] data;

    private double prevWidth;
    private double prevHeight;

    private void Start()
    {
        height = width * (float)Screen.height / Screen.width;
        data = new Data[1];

        data[0] = new Data
        {
            w = width,
            h = height,
            r = rStart,
            i = iStart,
            screenHeight = Screen.height,
            screenWidth = Screen.width
        };

        buffer = new ComputeBuffer(data.Length, 40);
        texture = new RenderTexture(Screen.width, Screen.height, 0);
        texture.enableRandomWrite = true;
        texture.Create();
        
        Mandelbrot();
    }

    private void Update()
    {
        if (Input.GetMouseButton(0))
        {
            ZoomIn();
        }       
        else if (Input.GetMouseButton(1))
        {
            ZoomOut();
        }        
        if (Input.GetMouseButtonDown(2))
        {
            CenterScreen();
        }
    }

    void Mandelbrot()
    {
        int kernelHandle = shader.FindKernel("CSMain");
        
        buffer.SetData(data);
        shader.SetBuffer(kernelHandle, "buffer", buffer);
        shader.SetInt("maxIterations", maxIterations);
        shader.SetTexture(kernelHandle, "Result", texture);
        
        shader.Dispatch(kernelHandle, Screen.width / 6, Screen.height / 6, 1);

        RenderTexture.active = texture;
        image.material.mainTexture = texture;
    }

    private void CenterScreen()
    {
        rStart += (Input.mousePosition.x - Screen.width / 2.0f) / Screen.width * width;
        iStart += (Input.mousePosition.y - Screen.height / 2.0f) / Screen.height * height;

        data[0].r = rStart;
        data[0].i = iStart;
        
        Mandelbrot();
    }

    private void ZoomIn()
    {
        maxIterations = Mathf.Max(100, maxIterations + increment);

        double wFactor = width * zoom * Time.deltaTime;
        double hFactor = height * zoom * Time.deltaTime;
        width -= wFactor;
        height -= hFactor;

        rStart += wFactor / 2.0;
        iStart += hFactor / 2.0;

        data[0].w = width;
        data[0].h = height;
        data[0].r = rStart;
        data[0].i = iStart;
        
        Mandelbrot();
    }
    
    private void ZoomOut()
    {
        maxIterations = Mathf.Max(100, maxIterations - increment);

        double wFactor = width * zoom * Time.deltaTime;
        double hFactor = height * zoom * Time.deltaTime;
        width += wFactor;
        height += hFactor;

        rStart -= wFactor / 2.0;
        iStart -= hFactor / 2.0;

        data[0].w = width;
        data[0].h = height;
        data[0].r = rStart;
        data[0].i = iStart;
        
        Mandelbrot();
    }

    private void OnDestroy()
    {
        buffer.Dispose();
    }
}