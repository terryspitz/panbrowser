///Terry Spitz (c)2006
///Simulate Caustics at the bottom of a flat pool
///
//TODO:
//


using System;

namespace Terry
{
    public class CausticCoefficients
    {
        /// <summary>
        /// replace Random with this NonRandom class to provide deterministic testing
        /// </summary>
        class NonRandom
        {
            public double NextDouble() { return 0.1; }
        }

        static public CausticCoefficients Instance = new CausticCoefficients(6);
        public double[, ,] Coeffs;
        public CausticCoefficients(int freqs)
        {
            Coeffs = new double[freqs, freqs, 1];
            var r = new Random();
            for (int i = 0; i < Coeffs.GetLength(0); i++)
                for (int j = 0; j < Coeffs.GetLength(1); j++)
                    for (int k = 0; k < Coeffs.GetLength(2); k++)
                        //Coeffs[i, j] = (double)(r.NextDouble() - 0.5) * 1f / ((i + 1) * (i + 1) + (j + 1) * (j + 1));
                        Coeffs[i, j, k] = (double)(r.NextDouble() - 0.5) * 1f / ((i + 1) + (j + 1) + (k + 1));
        }
    }

    public class Caustic
    {
        public static Caustic Instance = new Caustic();
        protected double[, ,] Coeffs;
        protected int step;
        protected double time = 0;
        const int freqs = 6;
        const double prescale = 10f;
        const double postscale = 200;
        const double bump = 0.001f;
        const double speed = 0.02f;

        public Caustic()
        {
            Coeffs = CausticCoefficients.Instance.Coeffs;
        }

        /// from: http://stackoverflow.com/questions/523531/fast-transcendent-trigonometric-functions-for-java
        // Return an approx to sin(pi/2 * x) where -1 <= x <= 1.
        // In that range it has a max absolute error of 5e-9
        // according to Hastings, Approximations For Digital Computers.
        /// <summary>
        ///  doesn't work cos numerical approximation is the whole issue, plus no faster anyway.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static double Sin(double x)
        {
            x = x % 1.0;
            double x2 = x * x;
            return ((((.00015148419 * x2
                       - .00467376557) * x2
                      + .07968967928) * x2
                     - .64596371106) * x2
                    + 1.57079631847) * x;
        }

        public double Calc(double x, double y, double z, double time)
        {
            double bumpedVal = WaveHeight(x / prescale, y / prescale, z / prescale, time) - WaveHeight(x / prescale + bump, y / prescale, z / prescale, time);
            double ydtl = WaveHeight(x / prescale, y / prescale, z / prescale, time) - WaveHeight(x / prescale, y / prescale + bump, z / prescale, time);
            double zdtl = WaveHeight(x / prescale, y / prescale, z / prescale, time) - WaveHeight(x / prescale, y / prescale, z / prescale + bump, time);

            bumpedVal *= postscale; ydtl *= postscale; zdtl *= postscale;
            //return (x + bumpedVal) * (x + bumpedVal) + (y + ydtl) * (y + ydtl) + z * z; //simpler
            return (x + bumpedVal) * (x + bumpedVal) + (y + ydtl) * (y + ydtl) + (z + zdtl) * (z + zdtl); //smooth, mmm.
            //return Math.Abs(x + bumpedVal) + Math.Abs(y + ydtl) + Math.Abs(z + zdtl);  //squarer
        }

        public double WaveHeight(double x, double y, double z, double time)
        {
            double f = 0;
            const double pi2 = (double)(2 * Math.PI);
            for (int i = 0; i < Coeffs.GetLength(0); i++)
                for (int j = 0; j < Coeffs.GetLength(1); j++)
                    for (int k = 0; k < Coeffs.GetLength(2); k++)
                        f += Coeffs[i, j, k] * 
                            (double)Math.Sin(
                                    pi2 *
                                    //((x+1 + Speed[i, 0] * time) * i + (y+1 + Speed[i, 1] * time) * j));
                                    ((x + speed * time) * i + (y + speed * time) * j + (z + speed * time) * k)
                                );
            return f;
        }

        // 1D version
        public double Calc1D(double x, double y, double z, int axis, double time, double prescale, double postscale, double bump)
        {
            double bumpedVal=0;
            if(axis == 0)
                bumpedVal = WaveHeight(x / prescale, y / prescale, z / prescale, time) - WaveHeight(x / prescale + bump, y / prescale, z / prescale, time);
            else if (axis == 1)
                bumpedVal = WaveHeight(x / prescale, y / prescale, z / prescale, time) - WaveHeight(x / prescale, y / prescale + bump, z / prescale, time);
            else if (axis == 2)
                bumpedVal = WaveHeight(x / prescale, y / prescale, z / prescale, time) - WaveHeight(x / prescale, y / prescale, z / prescale + bump, time);

            bumpedVal *= postscale;
            return bumpedVal;
        }
    }
}