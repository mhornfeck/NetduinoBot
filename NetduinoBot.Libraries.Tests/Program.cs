using System;
using Microsoft.SPOT;

namespace NetduinoBot.Libraries.Tests
{
  public class Program
  {
    public static void Main()
    {
      var encoderTests = new QuadratureEncoderTests();
      encoderTests.ProcessReadings_SteadyClockwiseRotation();
    }
  }

  public class QuadratureEncoderTests
  {
    public void ProcessReadings_SteadyClockwiseRotation()
    {
      var encoder = new QuadratureEncoder(EncodingTypes.X2);

      var chanA = 0;
      var chanB = 0;

      for (int i = 0; i < 20; i++)
      {
        chanA = chanA == 0 ? 1 : 0;
        chanB = chanB == 0 ? 1 : 0;

        encoder.ProcessReadings(chanA, chanB);

        Debug.Print("Pulses: " + encoder.Pulses);
        Debug.Print("Revs: " + encoder.Revolutions);
        Debug.Print("Current State: " + encoder.CurrentState);
        Debug.Print("---");
      }
    }
  }
}
