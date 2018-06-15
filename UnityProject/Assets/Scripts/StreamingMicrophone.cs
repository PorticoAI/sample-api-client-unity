using System;
using System.Collections.Generic;
using UnityEngine;

public class StreamingMicrophone : MonoBehaviour
{
  public PorticoConversation Conversation;
  public UIBinding UIBinding;
  public AudioSource TestSource;
  public bool MicrophoneAutoToggle = false;
  public float NoiseThreshold = 0.2f;
  public float QuietTime = 0.5f;
  
  const float ChunkTime = 0.1f;
  AudioClip m_recording;
  int m_lastSample = 0;
  float m_threshold;
  List<byte> m_buffer;
  float m_timer;
  float m_quietTimer;
  public float m_noiseLevel;

  void Start()
  {
    if(Microphone.devices.Length == 0)
    {
      Debug.LogError("No microphone available.");
      return;
    }

    m_buffer = new List<byte>();

    if (MicrophoneAutoToggle)
      ToggleMicrophone();
  }

  public void ToggleMicrophone()
  {
    if (!Microphone.IsRecording(null))
    {
      m_recording = Microphone.Start(null, true, 15, 44100);

      Conversation.StartStreaming();

      if(m_recording != null)
      {
        Debug.LogFormat("Mic on: {0}", Microphone.devices[0]);
      }

      UIBinding.OnStreamingStarted();
    }
    else
    {
      Microphone.End(null);
      Conversation.StopStreaming();

      TestSource.clip = m_recording;
      TestSource.Play();

      UIBinding.OnStreamingStopped();
    }
  }

  void FixedUpdate()
  {
    if (!Microphone.IsRecording(null))
    {
      return;
    }
    
    int pos = Microphone.GetPosition(null);
    int diff = pos - m_lastSample;

    if (diff > 0)
    {
      float[] samples = new float[diff * m_recording.channels];

      m_recording.GetData(samples, m_lastSample);
      var noiseTotal = 0.0f;
      for(int i = 0; i < samples.Length; i++)
      {
        noiseTotal += Mathf.Abs(samples[i]);
      }

      var noiseAvg = noiseTotal / samples.Length;

      // Smooth the noise level to ignore pops.
      m_noiseLevel = noiseAvg * 0.1f + m_noiseLevel * 0.9f;
      
      OnSamples(samples);
    }

    m_lastSample = pos;

    if (m_buffer.Count > 0)
    {
      m_timer += Time.deltaTime;
    }

    if (m_timer >= ChunkTime)
    {
      Conversation.OnPredictSamples(m_buffer.ToArray());
      m_timer = 0.0f;
      m_buffer.Clear();
    }

    if(m_noiseLevel > NoiseThreshold)
    {
      m_quietTimer = 0.0f;

      if (MicrophoneAutoToggle)
        Conversation.StartStreaming();
    }
    else
    {
      m_quietTimer += Time.deltaTime;
    }

    if (m_quietTimer > QuietTime)
    {
      m_quietTimer = 0.0f;

      if (MicrophoneAutoToggle)
      {
        Conversation.StopStreaming();
      }
    }
  }

  void OnSamples(float [] samples)
  {
    var buffer = ConvertTo16BitBytes(samples);

    m_buffer.AddRange(buffer);
  }

  byte[] ConvertTo16BitBytes(float[] samples)
  {
    Int16[] intData = new Int16[samples.Length];
    Byte[] bytesData = new Byte[samples.Length * 2];
    var rescaleFactor = 0x0FFF;

    for (int i = 0; i < samples.Length; i++)
    {
      intData[i] = (short)(Mathf.Min(samples[i], 1.0f) * rescaleFactor);
      Byte[] byteArr = new Byte[2];
      byteArr = BitConverter.GetBytes(intData[i]);
      byteArr.CopyTo(bytesData, i * 2);
    }

    return bytesData;
  }
}
