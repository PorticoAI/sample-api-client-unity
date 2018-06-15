using PorticoTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using WebSocketSharp;

public class PorticoConversation : MonoBehaviour
{
  public string MODEL_ID;
  public string MODEL_NAME;
  public string MY_TOKEN;
  public TextAsset TrainingFile;
  public UIBinding UIBinding;

  WebSocket m_ws;
  string API_BASE = "dev-train.porticotraining.com/api";
  Dictionary<string, string> m_headers;
  IntentResponses m_responses;

  void Start()
  {
    m_headers = new Dictionary<string, string>();
    m_headers["Authorization"] = string.Format("Bearer {0}", MY_TOKEN);
    m_headers["Content-Type"] = "application/json";    
    
    UIBinding.OnModelName(MODEL_NAME);
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
    };

    m_ws.OnMessage += (sender, e) => {
      var rtPrediction = JsonUtility.FromJson<RealTimePredictionResult>(e.Data);
      UIBinding.OnReceivedRealtimeResult(rtPrediction);
      };

    m_ws.OnError += (sender, e) =>
    {
      Debug.LogErrorFormat("WebSocket Error {0}", e.Message);
    };

    m_ws.OnClose += (sender, e) =>
    {
      Debug.Log("WebSocket Closed");
    };

    m_ws.Connect();
  }

  public void StartStreaming()
  {
    if (m_ws == null)
    {
      Debug.LogError("Stream not connected.");
      return;
    }

    var startMsg = new StartStreamingPayload(44100);
    var msg = JsonUtility.ToJson(startMsg);
    m_ws.Send(msg);
  }

  public void StopStreaming()
  {
    if (m_ws == null)
    {
      Debug.LogError("Stream not connected.");
      return;
    }

    var stopStreaming = new StopStreamingPayload();
    var msg = JsonUtility.ToJson(stopStreaming);
    m_ws.Send(msg);
  }

  public void OnPredictSamples(byte[] samples)
  {
    m_ws.Send(samples);
  }
}
