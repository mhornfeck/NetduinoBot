using System;
using System.Threading;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;

namespace Kenbot
{
  public static class Program
  {
    static readonly OutputPort led = new OutputPort(Pins.ONBOARD_LED, false);
    
    public static void Main()
    {
      var analog0 = new AnalogInput(Cpu.AnalogChannel.ANALOG_0);
      var button = new InterruptPort(Pins.ONBOARD_SW1, false, Port.ResistorMode.Disabled, Port.InterruptMode.InterruptEdgeBoth);
      button.OnInterrupt += button_OnInterrupt;

      var wheelSpeed = new PWM(PWMChannels.PWM_PIN_D5, 100, 1.0, false);
      var wheelDirection = new OutputPort(Pins.GPIO_PIN_D4, true);

      var counter = 0;

      wheelSpeed.Start();

      while (true)
      {
        var input = analog0.Read();
        led.Write(input < 0.2);

        counter++;

        if (counter % 100 == 0)
        {
          ChangeMotorSpeed(counter, wheelSpeed);
        }

        if (counter >= 2000)
        {
          counter = 0;
        }

        Thread.Sleep(10);
      }
    }

    static void ChangeMotorSpeed(int counter, PWM pwm)
    {
      var modifier = counter/100;

      if (modifier > 10)
      {
        modifier = 10 + (10 - modifier);
      }

      var dutyCycle = (10.0 + (10.0*modifier)) / 100.0;

      if (dutyCycle >= 0.0 && dutyCycle <= 1.0)
      {
        pwm.DutyCycle = dutyCycle;
      }
    }

    static void button_OnInterrupt(uint data1, uint data2, DateTime time)
    {
      // data1 = number of pin (onboard switch)
      // data2 = indicates pushed or released

      // led.Write(data2 == 0);
    }
  }
}
