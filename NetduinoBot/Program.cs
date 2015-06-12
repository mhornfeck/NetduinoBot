using System;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using NetduinoBot.Libraries;
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

		//private static readonly QuadratureEncoder _wheelEncoder = new QuadratureEncoder(Pins.GPIO_PIN_D13,
		//	Pins.GPIO_PIN_D12, EncodingTypes.X2);

		private static readonly Encoder _wheelEncoder = new Encoder(Pins.GPIO_PIN_D12,
			Pins.GPIO_PIN_D13);

		private const double Rate = 0.1;

		public static void Main()
		{
			_button.OnInterrupt += button_OnInterrupt;
			_wheelMotor.Start();
			_wheelController.Enable();

			var timer = 0;

			while (_button.Read() == false && Debug.GC(true) > 5000)
			{
        //Debug.Print("Time: " + timer + "ms. Pulses: " + _wheelEncoder.Pulses);
				//Debug.Print("Time: " + timer + "ms. Pulses: " + _wheelEncoder.Position);
				//Debug.Print(Debug.GC(true).ToString());
				//Thread.Sleep(100);
			}


		}

		private static double GetProcessVariable()
		{
			//var pulses = _wheelEncoder.Pulses;
			var pulses = _wheelEncoder.Position;
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

			_wheelMotor.DutyCycle = 0;
			_wheelController.Disable();
			_wheelMotor.Stop();
		}
	}
}
