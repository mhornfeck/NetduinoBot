using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;

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

		public QuadratureEncoder(Cpu.Pin channelAPin, Cpu.Pin channelBPin, EncodingTypes encoding)
			: this(encoding)
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

			Debug.Print("Current State: " + _currentState);
			Debug.Print("Previous State: " + _previousState);

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

			Debug.Print("Pulses: " + Pulses);
			Debug.Print(Debug.GC(true).ToString());
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

	/// <summary>
	/// For use with quadrature encoders where signal A and B are 90° out of phase.
	/// </summary>
	/// <remarks>
	/// For a detented rotary encoder such as http://www.sparkfun.com/products/9117
	/// Written by Michael Paauwe
	/// Tested with .NETMF 4.2RC1 and Netduino Plus FW4.2.0RC1
	/// </remarks>
	public class Encoder
	{
		private InputPort PhaseB;
		private InterruptPort PhaseA;
		private InterruptPort Button;
		public bool ButtonState = false;
		public int Position = 0;

		/// <summary>
		/// Constructor for encoder class
		/// </summary>
		/// <param name="pinA">The pin used for output A</param>
		/// <param name="pinB">The pin used for output B</param>
		/// <param name="pinButton">The pin used for the push contact (Optional:GPIO_NONE if not used)</param>
		public Encoder(Cpu.Pin pinA, Cpu.Pin pinB, Cpu.Pin pinButton = Pins.GPIO_NONE)
		{
			PhaseA = new InterruptPort(pinA, false, Port.ResistorMode.PullUp, Port.InterruptMode.InterruptEdgeBoth);
			PhaseA.OnInterrupt += PhaseA_OnInterrupt;
			//PhaseB = new InputPort(pinB, false, Port.ResistorMode.PullUp);
			PhaseB = new InterruptPort(pinB, false, Port.ResistorMode.PullUp, Port.InterruptMode.InterruptEdgeBoth);
			PhaseB.OnInterrupt += PhaseB_OnInterrupt;
		}

		void PhaseA_OnInterrupt(uint port, uint state, DateTime time)
		{
			Debug.Print("Phase A Interrupt. A=" + state + " B=" + PhaseB.Read());

			if (state == 0)
			{
				if (PhaseB.Read())
					Position++;
				else
					Position--;
			}
			else
			{
				if (PhaseB.Read())
					Position--;
				else
					Position++;
			}

			Debug.Print("Position: " + Position);
			Debug.Print(Debug.GC(true).ToString());
		}

		void PhaseB_OnInterrupt(uint port, uint state, DateTime time)
		{
			Debug.Print("Phase B Interrupt. A=" + PhaseA.Read() + " B=" + state);

			if (state == 0)
			{
				if (PhaseA.Read())
					Position++;
				else
					Position--;
			}
			else
			{
				if (PhaseA.Read())
					Position--;
				else
					Position++;
			}

			//Debug.Print("Position: " + Position);
			//Debug.Print(Debug.GC(true).ToString());
		}

		void Button_OnInterrupt(uint port, uint state, DateTime time)
		{
			ButtonState = (state == 0);
		}
	}
}