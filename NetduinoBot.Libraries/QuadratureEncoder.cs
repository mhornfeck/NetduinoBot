using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

namespace NetduinoBot.Libraries
{
  public class QuadratureEncoder : IRotaryEncoder
  {
    private readonly InterruptPort _channelA;
    private readonly InterruptPort _channelB;
    private readonly EncodingTypes _encoding;

    private int _currentState;
    private int _previousState;

    public int CurrentState { get { return _currentState; } }
    public int Pulses { get; private set; }
    public int Revolutions { get; private set; }

    private const int PrevMask = 0x1; //Mask for the previous state in determining direction of rotation.
    private const int CurrMask = 0x2; //Mask for the current state in determining direction of rotation.
    private const int Invalid = 0x3; //XORing two states where both bits have changed.

    public QuadratureEncoder(EncodingTypes encoding)
    {
      Pulses = 0;
      Revolutions = 0;

      _encoding = encoding;
    }

    public QuadratureEncoder(Cpu.Pin channelAPin, Cpu.Pin channelBPin, EncodingTypes encoding) : this(encoding)
    {
      _channelA = new InterruptPort(channelAPin, false, Port.ResistorMode.Disabled, Port.InterruptMode.InterruptEdgeBoth);
      _channelB = new InterruptPort(channelBPin, false, Port.ResistorMode.Disabled, Port.InterruptMode.InterruptEdgeBoth);

      _channelA.OnInterrupt += OnChannelInterrupt;

      if (_encoding == EncodingTypes.X4)
      {
        _channelB.OnInterrupt += OnChannelInterrupt;
      }
    }

    private void OnChannelInterrupt(uint port, uint state, DateTime time)
    {
      try
      {
        var chanA = _channelA.Read() ? 1 : 0;
        var chanB = _channelB.Read() ? 1 : 0;

        ProcessReadings(chanA, chanB);
      }
      catch (Exception e)
      {
        Debug.Print(e.Message);
      }
    }

    public void ProcessReadings(int chanA, int chanB)
    {
      _currentState = (chanA << 1) | (chanB);

      if (_encoding == EncodingTypes.X2)
      {
        // 11->00->11->00 is counter clockwise rotation or "forward".
        if ((_previousState == 0x3 && _currentState == 0x0) || (_previousState == 0x0 && _currentState == 0x3))
        {
          Pulses++;
        }
        // 10->01->10->01 is clockwise rotation or "backward".
        else if ((_previousState == 0x2 && _currentState == 0x1) || (_previousState == 0x1 && _currentState == 0x2))
        {
          Pulses--;
        }
      }
      else if (_encoding == EncodingTypes.X4)
      {
        // Entered a new valid state.
        if (((_currentState ^ _previousState) != Invalid) && (_currentState != _previousState))
        {
          // 2 bit state. Right hand bit of prev XOR left hand bit of current
          // gives 0 if clockwise rotation and 1 if counter clockwise rotation.
          var change = (_previousState & PrevMask) ^ ((_currentState & CurrMask) >> 1);

          if (change == 0)
          {
            change = -1;
          }

          Pulses -= change;
        }
      }

      _previousState = _currentState;
    }

    /// <summary>
    /// Reset the encoder. Set pulses and revolutions count to zero.
    /// </summary>
    public void Reset()
    {
      Pulses = 0;
      Revolutions = 0;
    }
  }
}