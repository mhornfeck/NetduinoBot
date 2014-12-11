using System;
using System.Threading;
using Microsoft.SPOT;
using Math = System.Math;

namespace Kenbot
{
  public delegate double GetDouble();
  public delegate void SetDouble(double value);

  public class PIDController
  {
    #region Fields

    //Gains
    private double _kp;
    private double _ki;
    private double _kd;

    //Running Values
    private DateTime _lastUpdate = DateTime.MinValue;
    private double _lastPv;
    private double _errSum;

    //Reading/Writing Values
    private GetDouble _readPv;
    private GetDouble _readSp;
    private SetDouble _writeOv;

    //Max/Min Calculation
    private double _pvMax;
    private double _pvMin;
    private double _outMax;
    private double _outMin;

    //Threading and Timing
    private const double ComputeHz = 1.0f;
    private Thread _runThread;

    #endregion

    #region Properties

    public double PGain
    {
      get { return _kp; }
      set { _kp = value; }
    }

    public double IGain
    {
      get { return _ki; }
      set { _ki = value; }
    }

    public double DGain
    {
      get { return _kd; }
      set { _kd = value; }
    }

    public double PVMin
    {
      get { return _pvMin; }
      set { _pvMin = value; }
    }

    public double PVMax
    {
      get { return _pvMax; }
      set { _pvMax = value; }
    }

    public double OutMin
    {
      get { return _outMin; }
      set { _outMin = value; }
    }

    public double OutMax
    {
      get { return _outMax; }
      set { _outMax = value; }
    }

    public bool PIDOK
    {
      get { return _runThread != null; }
    }

    #endregion

    #region Construction / Deconstruction

    public PIDController(double pG, double iG, double dG,
        double pMax, double pMin, double oMax, double oMin,
        GetDouble pvFunc, GetDouble spFunc, SetDouble outFunc)
    {
      _kp = pG;
      _ki = iG;
      _kd = dG;
      _pvMax = pMax;
      _pvMin = pMin;
      _outMax = oMax;
      _outMin = oMin;
      _readPv = pvFunc;
      _readSp = spFunc;
      _writeOv = outFunc;
    }

    ~PIDController()
    {
      Disable();
      _readPv = null;
      _readSp = null;
      _writeOv = null;
    }

    #endregion

    #region Public Methods

    public void Enable()
    {
      if (_runThread != null)
        return;

      Reset();

      _runThread = new Thread(Run);
      _runThread.Start();
    }

    public void Disable()
    {
      if (_runThread == null)
        return;

      _runThread.Abort();
      _runThread = null;
    }

    public void Reset()
    {
      _errSum = 0.0f;
      _lastUpdate = DateTime.Now;
    }

    #endregion

    #region Private Methods

    private double ScaleValue(double value, double valuemin, double valuemax, double scalemin, double scalemax)
    {
      var vPerc = (value - valuemin) / (valuemax - valuemin);
      var bigSpan = vPerc * (scalemax - scalemin);

      var retVal = scalemin + bigSpan;

      return retVal;
    }

    private double Clamp(double value, double min, double max)
    {
      if (value > max)
      {
        return max;
      }
      if (value < min)
      {
        return min;
      }

      return value;
    }

    private void Compute()
    {
      if (_readPv == null || _readSp == null || _writeOv == null)
        return;

      var pv = _readPv();
      var sp = _readSp();

      //We need to scale the pv to +/- 100%, but first clamp it
      pv = Clamp(pv, _pvMin, _pvMax);
      pv = ScaleValue(pv, _pvMin, _pvMax, -1.0f, 1.0f);

      //We also need to scale the setpoint
      sp = Clamp(sp, _pvMin, _pvMax);
      sp = ScaleValue(sp, _pvMin, _pvMax, -1.0f, 1.0f);

      //Now the error is in percent...
      var err = sp - pv;

      var pTerm = err * _kp;
      double iTerm = 0.0f;
      double dTerm = 0.0f;

      double partialSum = 0.0f;
      var nowTime = DateTime.Now;

      if (_lastUpdate != DateTime.MinValue)
      {
        double dT = (nowTime - _lastUpdate).Seconds;

        //Compute the integral if we have to...
        if (pv >= _pvMin && pv <= _pvMax)
        {
          partialSum = _errSum + dT * err;
          iTerm = _ki * partialSum;
        }

        if (Math.Abs(dT) > 0.000001)
        {
          dTerm = _kd*(pv - _lastPv)/dT;
        }
      }

      _lastUpdate = nowTime;
      _errSum = partialSum;
      _lastPv = pv;

      //Now we have to scale the output value to match the requested scale
      var outReal = pTerm + iTerm + dTerm;

      outReal = Clamp(outReal, -1.0f, 1.0f);
      outReal = ScaleValue(outReal, -1.0f, 1.0f, _outMin, _outMax);

      //Write it out to the world
      _writeOv(outReal);
    }

    #endregion

    #region Threading

    private void Run()
    {

      while (true)
      {
        try
        {
          const int sleepTime = (int)(1000 / ComputeHz);
          Thread.Sleep(sleepTime);
          Compute();
        }
        catch (Exception e)
        {
          Debug.Print(e.Message);
        }
      }

    }

    #endregion
  }
}


