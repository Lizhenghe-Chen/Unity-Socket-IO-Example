using System;
using System.Collections.Generic;
using SocketIOClient;
using SocketIOClient.Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

namespace UnityClient
{
    public class SocketIOManager : MonoBehaviour
    {
        private SocketIOUnity _socket;
        public ServerIPAddress serverIPAddress = ServerIPAddress.Localhost;
        public int port = 11100;

        [SerializeField] private Button sendMissionButton;
        [SerializeField] private TMPro.TextMeshProUGUI serverMessage;
        public Person personData = new();

        public enum ServerIPAddress
        {
            Localhost,
            BunnyChenServer
        }

        private void Awake()
        {
            sendMissionButton.onClick.AddListener(SendMissionRequest);
            // DontDestroyOnLoad(gameObject); // additional feature
        }

        private void Start()
        {
            InitConnection();
            RegisterSocketEvents();
            Debug.Log("Connecting...");
            _socket.Connect();
        }

        private void OnDisable() => _socket?.Disconnect();

        private void InitConnection()
        {
            _socket = new SocketIOUnity(new Uri($"http://{GetIPAddress()}:{port}"), new SocketIOOptions
            {
                Query = new Dictionary<string, string> { { "token", "UNITY" } },
                ReconnectionDelay = 5000,
                ReconnectionAttempts = 5
            })
            {
                JsonSerializer = new NewtonsoftJsonSerializer()
            };
        }

        private void RegisterSocketEvents()
        {
            _socket.OnAnyInUnityThread(DebugServerData);
            _socket.OnConnected += (_, _) => Debug.Log($"<color=green>Connected to {_socket.ServerUri}</color>");
            _socket.OnDisconnected += (_, _) => Debug.Log("<color=red>Disconnected</color>");
            _socket.OnError += (_, error) => Debug.Log($"<color=red>Error: {error}</color>");
            _socket.OnReconnected += (_, _) => Debug.Log("<color=green>Reconnected</color>");
            _socket.OnReconnectFailed += (_, _) => Debug.Log("<color=red>Reconnect Failed</color>");
            _socket.OnReconnectAttempt += (_, attempt) =>
                Debug.Log($"<color=yellow>Reconnect Attempt: {attempt}</color>");

            _socket.OnUnityThread("hello", response => serverMessage.text = response.GetValue().GetRawText());
            _socket.OnUnityThread("mission", response => serverMessage.text = response.ToString());
            _socket.OnUnityThread("data_transfer", ConvertData);
        }

        private void ConvertData(SocketIOResponse response)
        {
            try
            {
                personData = JsonUtility.FromJson<Person>(response.GetValue().GetRawText());
                serverMessage.text += $"\nName: {personData.name}, Age: {personData.age}, Email: {personData.email}";
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to deserialize JSON data: {ex.Message}");
            }
        }

        private void SendMissionRequest() =>
            Emit("mission_request", "Hi, I am Unity Client, and ready to receive the mission!");

        private static void DebugServerData(string eventName, SocketIOResponse response) =>
            Debug.Log($"<color=blue>Event Name: {eventName}</color>, {response}");

        private void Emit(string eventName, params object[] data)
        {
            _socket.Emit(eventName, data);
            Debug.Log(
                $"<color=orange>Event Name: {eventName}</color>, Data: {string.Join(", ", data ?? Array.Empty<object>())}");
        }

        private string GetIPAddress() => serverIPAddress switch
        {
            ServerIPAddress.Localhost => "127.0.0.1",
            ServerIPAddress.BunnyChenServer => "10.??.??.??",
            _ => "localhost"
        };

        [Serializable]
        public class Person
        {
            public string name;
            public int age;
            public string email;
        }
    }
}