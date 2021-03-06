﻿using PorticoTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using WebSocketSharp;

public class PorticoConversation : MonoBehaviour
{
  public bool AutoConnect = true;
  public string MODEL_ID;
  public string MODEL_NAME;
  public string MY_TOKEN;
  public string API_BASE = "api.porticotraining.com";
  public TextAsset TrainingFile;
  public UIBinding UIBinding;

  enum STT_State
  {
    Disconnected,
    Started,
    Stopped,
  }


  WebSocket m_ws;
  Dictionary<string, string> m_headers;
  IntentResponses m_responses;
  STT_State m_sttState;

  void Start()
  {
    m_headers = new Dictionary<string, string>();
    m_headers["Authorization"] = string.Format("Bearer {0}", MY_TOKEN);
    m_headers["Content-Type"] = "application/json";
    m_sttState = STT_State.Disconnected;


    UIBinding.OnModelName(MODEL_NAME);

    if(AutoConnect)
      ConnectToStreamingServer();
  }
  
  public void StartCreateModel()
  {
    StartCoroutine(CreateModel());
  }

  IEnumerator CreateModel()
  {
    var url = string.Format("https://{0}/model", API_BASE);
    var payload = new CreateModelPayload(MODEL_NAME, "en-us");
    var payloadJson = JsonUtility.ToJson(payload);
    var bodyBytes = Encoding.UTF8.GetBytes(payloadJson);
    
    UIBinding.OnModelResult("Creating Model");
    UIBinding.OnModelName(MODEL_NAME);

    using (WWW www = new WWW(url, bodyBytes, m_headers))
    {
      yield return www;

      UIBinding.OnModelResult("Created Model Status: " + www.text);
      var createModelResult = JsonUtility.FromJson<CreateModelResult>(www.text);
      if (createModelResult != null)
      {
        MODEL_ID = createModelResult.id;
      }
      else
      {
        Debug.Log("Response is null");
      }
    }
  }

  public void StartTrainModel()
  {
    StartCoroutine(TrainModel());
  }

  public IEnumerator TrainModel()
  {
    var url = string.Format("https://{0}/model/{1}/train", API_BASE, MODEL_ID);
    var intents = TrainingFile.text.Split('\n');
    var payload = new TrainModelPayload(intents);
    var payloadJson = JsonUtility.ToJson(payload);
    var bodyBytes = Encoding.UTF8.GetBytes(payloadJson);

    UIBinding.OnModelResult("Training Model");
    using (WWW www = new WWW(url, bodyBytes, m_headers))
    {
      yield return www;

      UIBinding.OnModelResult("Training Status: " + www.text);
    }
  }

  public void StartModelStatus()
  {
    StartCoroutine(ModelStatus());
  }

  public IEnumerator ModelStatus()
  {
    var url = string.Format("https://{0}/model/{1}", API_BASE, MODEL_ID);

    UIBinding.OnModelResult("Getting Model Status");
    using (WWW www = new WWW(url, null, m_headers))
    {
      yield return www;

      UIBinding.OnModelResult("Model Status: " + www.text);
    }
  }

  public void StartPredictText()
  {
    var statements = UIBinding.GetPredictStatements();
    StartCoroutine(PredictFromText(statements));
  }

  public IEnumerator PredictFromText(string [] statements)
  {
    var url = string.Format("https://{0}/model/{1}/predict-from-text", API_BASE, MODEL_ID);    
    var payload = new PredictTextPayload(statements);
    var payloadJson = JsonUtility.ToJson(payload);
    var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);

    UIBinding.OnModelResult("Predicting");

    using (WWW www = new WWW(url, payloadBytes, m_headers))
    {
      yield return www;

      UIBinding.OnTextPredictResult(www.text);
    }
  }

  public void OnPredictText(string [] statements)
  {
    StartCoroutine(PredictFromText(statements));
  }

  public void ConnectToStreamingServer()
  {
    if(MY_TOKEN == string.Empty)
    {
      Debug.LogError("Token cannot be empty when trying to connect to the server.");
      return;
    }

    var wsUrl = string.Format(
                  "wss://{0}/model/{1}/predict-rt?token={2}&interim=true&user_id={3}",
                  API_BASE,
                  MODEL_ID,
                  MY_TOKEN,
                  "username");

    m_ws = new WebSocket(wsUrl);

    m_ws.OnOpen += (sender, e) =>
    {
      Debug.Log("WebSocket connected");
      m_sttState = STT_State.Stopped;
      UIBinding.OnWSConnected();
    };

    m_ws.OnMessage += (sender, e) => {
      var message = JsonUtility.FromJson<ServerToClientMessage>(e.Data);
      switch (message.type)
      {
        case "ready":
          {
            var streamingStatus = JsonUtility.FromJson<StreamingServerStatus>(e.Data);
            UIBinding.OnConnectionResult(streamingStatus);
          }
          break;
        case "failure":
          {
            var streamingStatus = JsonUtility.FromJson<StreamingServerStatus>(e.Data);
            UIBinding.OnConnectionResult(streamingStatus);
            Debug.LogErrorFormat("Server ({0}) not ready.", streamingStatus.id);
          }
          break;
        case "intent":
          {
            var rtPrediction = JsonUtility.FromJson<RealTimePredictionResult>(e.Data);
            UIBinding.OnReceivedRealtimeResult(rtPrediction);
          }
          break;
      }
    };

    m_ws.OnError += (sender, e) =>
    {
      Debug.LogErrorFormat("WebSocket Error {0}", e.Message);
      UIBinding.OnWSError(e.Message);
    };

    m_ws.OnClose += (sender, e) =>
    {
      Debug.Log("WebSocket Closed");
      UIBinding.OnWSDisconnected();
    };

    m_ws.Connect();
  }

  public void StartStreaming()
  {
    if (m_ws == null)
    {
      Debug.Log("Stream not connected.");
      return;
    }

    if (m_sttState == STT_State.Started)
      return;

    var startMsg = new StartStreamingPayload(44100);
    var msg = JsonUtility.ToJson(startMsg);
    m_ws.Send(msg);

    m_sttState = STT_State.Started;

    UIBinding.OnStreamingStarted();
  }

  public void StopStreaming()
  {
    if (m_ws == null)
    {
      Debug.Log("Stream not connected.");
      return;
    }

    if (m_sttState == STT_State.Stopped)
      return;

    var stopStreaming = new StopStreamingPayload();
    var msg = JsonUtility.ToJson(stopStreaming);
    m_ws.Send(msg);

    m_sttState = STT_State.Stopped;

    UIBinding.OnStreamingStopped();
  }

  public void OnPredictSamples(byte[] samples)
  {
    if(m_sttState == STT_State.Started)
      m_ws.Send(samples);
  }
}
