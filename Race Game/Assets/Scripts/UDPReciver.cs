using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UDPReceiver : MonoBehaviour
{
    [Header("Network Settings")]
    public int port = 5005;

    [Header("Target Objects")]
    public Transform realLifeCar; 
    public TextMeshProUGUI liveHUDText;     

    [Header("Calibration Boundary Settings")]
    public float maxInputX = 1280f; 
    public float maxInputY = 720f; 

    public float unityScaleX = 25f;
    public float unityScaleZ = 15f;
    public float lerpSpeed = 12f;

    private UdpClient client;
    private Thread receiveThread;
    private string lastReceivedPacket = "";
    private bool isRunning = true;

    private Vector3 targetPosition;
    private Vector3 lastPosition;
    private int currentLap = 0;

    [System.Serializable]
    public class CarTelemetry
    {
        public string car_name;
        public int x;
        public int y;
        public int lap;
        public float lap_time;
    }

    void Start()
    {
        if (realLifeCar != null) targetPosition = realLifeCar.position;

        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    private void ReceiveData()
    {
        client = new UdpClient(port);
        while (isRunning)
        {
            try
            {
                IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = client.Receive(ref anyIP);
                string text = Encoding.UTF8.GetString(data);

                lock (this)
                {
                    lastReceivedPacket = text;
                }
            }
            catch (Exception) { }
        }
    }

    void Update()
    {
        string packetToProcess = "";

        lock (this)
        {
            if (!string.IsNullOrEmpty(lastReceivedPacket))
            {
                packetToProcess = lastReceivedPacket;
                lastReceivedPacket = "";
            }
        }

        if (!string.IsNullOrEmpty(packetToProcess))
        {
            ProcessRealLifeCar(packetToProcess);
        }

        if (realLifeCar != null)
        {
            realLifeCar.position = Vector3.Lerp(realLifeCar.position, targetPosition, Time.deltaTime * lerpSpeed);

            Vector3 movementDirection = realLifeCar.position - lastPosition;
            if (movementDirection.magnitude > 0.01f)
            {
                movementDirection.y = 0;
                realLifeCar.rotation = Quaternion.LookRotation(movementDirection);
            }
            lastPosition = realLifeCar.position;
        }
    }

    private void ProcessRealLifeCar(string jsonString)
    {
        try
        {
            CarTelemetry telemetry = JsonUtility.FromJson<CarTelemetry>(jsonString);

            float normX = (telemetry.x / maxInputX) - 0.5f;
            float normY = (telemetry.y / maxInputY) - 0.5f;

            float targetX = normX * unityScaleX;
            float targetZ = -normY * unityScaleZ;

            targetPosition = new Vector3(targetX, realLifeCar.position.y, targetZ);

            if (liveHUDText != null)
            {
                liveHUDText.text = $"TRACKED VEHICLE: {telemetry.car_name}\n" +
                                   $"CURRENT LAP: {telemetry.lap}\n" +
                                   $"LAST LAP TIME: {telemetry.lap_time}s";
            }
        }
        catch (Exception) { }
    }

    void OnApplicationQuit()
    {
        isRunning = false;
        if (client != null) client.Close();
        if (receiveThread != null && receiveThread.IsAlive) receiveThread.Abort();
    }
}