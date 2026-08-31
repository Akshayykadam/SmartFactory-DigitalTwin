using System;
using UnityEngine;

namespace SmartFactory.DigitalTwin.Data
{
    public class MQTTClientBridge : MonoBehaviour
    {
        [Header("Broker Configuration")]
        [SerializeField] private string brokerAddress = "mqtt.smartfactory.internal";
        [SerializeField] private int brokerPort = 1883;
        [SerializeField] private string topicPrefix = "factory/telemetry/#";
        [SerializeField] private bool connectOnStart = false;

        [Header("State")]
        [SerializeField] private bool isConnected = false;

        public bool IsConnected => isConnected;

        public event Action<string, string> OnPayloadReceived;

        private void Start()
        {
            if (connectOnStart)
            {
                ConnectToBroker();
            }
        }

        public void ConnectToBroker()
        {
            Debug.Log($"[MQTT Bridge] Connecting to MQTT broker at {brokerAddress}:{brokerPort} with topic {topicPrefix}...");
            // In a live enterprise integration, connect via M2Mqtt / NativeWebSocket here
            isConnected = true;
        }

        public void Disconnect()
        {
            Debug.Log("[MQTT Bridge] Disconnected from broker.");
            isConnected = false;
        }

        public void PublishCommand(string topic, string payloadJson)
        {
            if (!isConnected)
            {
                Debug.LogWarning($"[MQTT Bridge] Cannot publish to {topic}: Not connected.");
                return;
            }
            Debug.Log($"[MQTT Bridge] Published to {topic}: {payloadJson}");
        }

        public void HandleIncomingRawJson(string topic, string jsonPayload)
        {
            OnPayloadReceived?.Invoke(topic, jsonPayload);
        }
    }
}
