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
        class NonRandom
        {
            public double NextDouble() { return 0.1; }
        }

        public float[, ,] Coeffs;
        public CausticCoefficients(int freqs)
        {
            Coeffs = new float[freqs, freqs, 1];
            var r = new Random();
            for (int i = 0; i < Coeffs.GetLength(0); i++)
                for (int j = 0; j < Coeffs.GetLength(1); j++)
                    for (int k = 0; k < Coeffs.GetLength(2); k++)
                        //Coeffs[i, j] = (float)(r.NextDouble() - 0.5) * 1f / ((i + 1) * (i + 1) + (j + 1) * (j + 1));
                        Coeffs[i, j, k] = (float)(r.NextDouble() - 0.5) * 1f / ((i + 1) + (j + 1) + (k + 1));
        }
    }

    public class Caustic
    {
        protected float[, ,] Coeffs;
        static protected CausticCoefficients staticCoeffs = new CausticCoefficients(freqs);
        protected int step;
        protected float time = 0;
        const int freqs = 6;
        const float prescale = 10f;
        const float postscale = 200;
        const float bump = 0.001f;
        const float speed = 0.02f;

        public Caustic()
        {
            Coeffs = staticCoeffs.Coeffs;
        }

        public float Calc(float x, float y, float z)
        {
            float xdtl = WaveHeight(x / prescale, y / prescale, z / prescale, time) - WaveHeight(x / prescale + bump, y / prescale, z / prescale, time);
            float ydtl = WaveHeight(x / prescale, y / prescale, z / prescale, time) - WaveHeight(x / prescale, y / prescale + bump, z / prescale, time);
            float zdtl = WaveHeight(x / prescale, y / prescale, z / prescale, time) - WaveHeight(x / prescale, y / prescale, z / prescale + bump, time);

            xdtl *= postscale; ydtl *= postscale; zdtl *= postscale; 
            //return (x + xdtl) * (x + xdtl) + (y + ydtl) * (y + ydtl) + z * z; //simpler
            return (x + xdtl) * (x + xdtl) + (y + ydtl) * (y + ydtl) + (z + zdtl) * (z + zdtl); //smooth, mmm.
            //return Math.Abs(x + xdtl) + Math.Abs(y + ydtl) + Math.Abs(z + zdtl);  //squarer
        }
        
        public float WaveHeight(float x, float y, float z, float time)
        {
            float f = 0;
            float pi2 = (float)(2 * Math.PI);
            for (int i = 0; i < Coeffs.GetLength(0); i++)
                for (int j = 0; j < Coeffs.GetLength(1); j++)
                    for (int k = 0; k < Coeffs.GetLength(2); k++)
                    f += Coeffs[i, j, k] * (float)Math.Sin(pi2*
                                //((x+1 + Speed[i, 0] * time) * i + (y+1 + Speed[i, 1] * time) * j));
                                ((x + speed * time) * i + (y + speed * time) * j + (z + speed * time) * k)
                            );
            return f;
        }

        public void Tick(double t)
        {
            time = (float)t;
        }
    }
}