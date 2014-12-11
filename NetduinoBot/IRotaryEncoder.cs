namespace Kenbot
{
  public interface IRotaryEncoder
  {
    int CurrentState { get; }
    int Pulses { get; }
    int Revolutions { get; }

    void Reset();
  }
}