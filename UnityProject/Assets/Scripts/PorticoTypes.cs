using System;

namespace PorticoTypes
{
  [Serializable]
  public class CreateModelPayload
  {
    public CreateModelPayload(string name, string language)
    {
      this.name = name;
      this.language = language;
    }

    public string name;
    public string language;
  }

  [Serializable]
  public class CreateModelResult
  {
    public string id;
  }

  [Serializable]
  public class TrainModelPayload
  {
    public TrainModelPayload(string[] intents)
    {
      this.intents = intents;
    }

    public string[] intents;
  }


  [Serializable]
  public class PredictTextPayload
  {
    public PredictTextPayload(string[] statements)
    {
      this.statements = statements;
    }

    public string[] statements;
  }

  [Serializable]
  public class PredictionResult
  {
    public SentenceResult[] sentences;
  }

  [Serializable]
  public class SentenceResult
  {
    public string statement;
    public Intent[] prediction;
  }

  [Serializable]
  public class Transcript
  {
    public string text;
    public float confidence;
    public float stability;
  }

  [Serializable]
  public class Intent
  {
    public string label;
    public float prediction;
  }

  [Serializable]
  public class StartStreamingPayload
  {
    public StartStreamingPayload(int sampleRate)
    {
      this.action = "start";
      this.sampleRate = sampleRate;
    }

    public string action;
    public int sampleRate;
  }


  [Serializable]
  public class StopStreamingPayload
  {
    public StopStreamingPayload()
    {
      this.action = "stop";
    }

    public string action;
  }


  [Serializable]
  public class ServerToClientMessage
  {
    public string type;
  }

  [Serializable]
  public class StreamingServerStatus
  {
    public string type;
    public string id;
  }

  [Serializable]
  public class RealTimePredictionResult
  {
    public string type;
    public Transcript transcript;
    public bool isFinal;
    public Intent[] intents;
  }

  [Serializable]
  public class IntentResponse
  {
    public string intent;
    public string response;
  }

  [Serializable]
  public class IntentResponses
  {
    public IntentResponse[] intentResponses;
  }
}