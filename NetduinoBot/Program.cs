using System;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;

namespace NetduinoBot
{
  public static class Program
  {
    private static readonly OutputPort _led = new OutputPort(Pins.ONBOARD_LED, false);
    private static readonly PWM _wheelMotor = new PWM(PWMChannels.PWM_PIN_D5, 20, 0, false);
    private static readonly OutputPort _wheelDirection = new OutputPort(Pins.GPIO_PIN_D4, true);
    private static readonly InterruptPort _button = new InterruptPort(Pins.ONBOARD_SW1, false, Port.ResistorMode.Disabled, Port.InterruptMode.InterruptEdgeBoth);

    private static readonly PIDController _wheelController = new PIDController(-0.715, 0.06, 0, 10667.0,
      0, 1.0, 0, GetProcessVariable, GetSetPoint, SetOutputValue);

    private static readonly QuadratureEncoder _wheelEncoder = new QuadratureEncoder(Pins.GPIO_PIN_D12, Cpu.Pin.GPIO_Pin13, EncodingTypes.X4);

    private const double Rate = 0.1;

    public static void Main()
    {
      _button.OnInterrupt += button_OnInterrupt;
      _wheelMotor.Start();
      _wheelController.Enable();

      var timer = 0;

      while (timer < 10000)
      {
        try
        {
          Debug.Print("Time: " + timer + "ms. Pulses: " + _wheelEncoder.Pulses);
          Thread.Sleep(100);
          timer += 100;
        }
        catch (Exception)
        {
          break;
        }
      }

      _wheelMotor.DutyCycle = 0;
      _wheelController.Disable();
    }

    private static double GetProcessVariable()
    {
      var pulses = _wheelEncoder.Pulses;
      var velocity = (pulses / Rate);
      return velocity;
    }

    private static double GetSetPoint()
    {
      return 3000;
    }

    private static void SetOutputValue(double value)
    {
      _wheelMotor.DutyCycle = value;
    }

    static void button_OnInterrupt(uint data1, uint data2, DateTime time)
    {
      // data1 = number of pin (onboard switch)
      // data2 = indicates pushed or released

      // led.Write(data2 == 0);
    }
  }
}
