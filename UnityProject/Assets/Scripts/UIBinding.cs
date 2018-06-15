using PorticoTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIBinding : MonoBehaviour
{
  public Text ModelResult;
  public Text ModelNameUI;
  public Text PredictStatements;
  public Text IntentResults;
  public Text Hypothesis;
  public Text Recognition;
  public Text Response;
  public Text MicrophoneButton;
  public TextAsset ResponseFile;

  Queue<RealTimePredictionResult> m_streamingResults;
  IntentResponses m_responses;

  void Start()
  {
    m_streamingResults = new Queue<RealTimePredictionResult>();

    LoadResponses();
  }

  void LoadResponses()
  {
    m_responses = JsonUtility.FromJson<IntentResponses>(ResponseFile.text);
  }

  public void OnModelName(string modelName)
  {
    ModelNameUI.text = modelName;
  }

  public void OnModelResult(string result)
  {
    ModelResult.text = result;
  }

  public string [] GetPredictStatements()
  {
    return PredictStatements.text.Split('\n');
  }

  public void OnTextPredictResult(string result)
  {
    var jsonArrayHack = string.Format("{{ \"sentences\": {0} }}", result);
    var response = "";
    var sentenceResults = JsonUtility.FromJson<PredictionResult>(jsonArrayHack);
    foreach (var sentence in sentenceResults.sentences)
    {
      foreach (var prediction in sentence.prediction)
      {
        var s = string.Format("label: {0} ({1:0.00})\n", prediction.label, prediction.confidence);
        response += s;
      }
    }

    OnModelResult(response);
  }

  public void OnReceivedRealtimeResult(RealTimePredictionResult rtPrediction)
  {
    m_streamingResults.Enqueue(rtPrediction);
  }

  void OnRealtimeResult(RealTimePredictionResult rtPrediction)
  {
    var response = string.Empty;
    foreach (var intent in rtPrediction.intents)
    {
      var s = string.Format("label: {0} ({1:0.00})\n", intent.label, intent.confidence);
      response += s;
    }

    Hypothesis.text = rtPrediction.text;
    if (rtPrediction.isFinal)
    {
      var newMsg = string.Format("{0}\n(label:{1})\n\n", rtPrediction.text, rtPrediction.intents[0].label);
      Recognition.text = newMsg + Recognition.text;

      foreach (var intentResponse in m_responses.intentResponses)
      {
        if (rtPrediction.intents[0].label == intentResponse.intent)
        {
          var newResponse = string.Format("{0}\n\n\n", intentResponse.response);
          Response.text = newResponse + Response.text;
          break;
        }
      }
    }

    IntentResults.text = response;
  }

  public void OnStreamingStarted()
  {
    MicrophoneButton.text = "Stop\nMicrophone";
  }

  public void OnStreamingStopped()
  {
    MicrophoneButton.text = "Start\nMicrophone";
  }

  void Update()
  {
    if (m_streamingResults.Count > 0)
    {
      OnRealtimeResult(m_streamingResults.Dequeue());
    }
  }
}
