using System;
using System.Collections;
using System.Collections.Generic;

namespace Terry
{
    /// <summary>
	/// This class caches images variables required by the F# code, 
	/// and returns the current values as set by the user in the GUI.
	/// </summary>
	public class Sliders
	{
		public class Variable 
		{ 
			public double val {get;set;}
            public double from { get; set; }
            public double to { get; set; }
			public Variable(double v, double f, double t) { val= v; from=f; to=t; }
            public bool shown = true;
            public override string ToString()
            {
                return string.Format("{0}:{1}-{2}", val, from, to);
            }
		}

        public DrWPF.Windows.Data.ObservableDictionary<string, Variable> variables = new DrWPF.Windows.Data.ObservableDictionary<string, Variable>();
		public static Sliders myUI = new Sliders();

		public Sliders()
		{
		}

		public static int Getdouble(string s, int from, int to)
		{
			return Getdouble(s, from, to, from);
		}
		public static int Getdouble(string s, int from, int to, int defaultValue)
		{
            lock (myUI)
            {
                Variable v;
                if (myUI.variables.TryGetValue(s, out v))
                {
                    v.shown = true;
                }
                else
                {
                    v = new Variable(defaultValue, from, to);
                    myUI.variables.Add(s, v);
                }
                return (int)v.val;
            }
		}
		public static Variable GetValues(string name)
		{
			return myUI.variables[name];
		}

		public static void SetValue(string name, double value)
		{
            lock (myUI)
            {
                Variable v = myUI.variables[name];
                v.val = value;
            }
		}

		public static double BumpValue(string name)
		{
            Variable v;
            lock (myUI)
            {
                if (myUI.variables.TryGetValue(name, out v))
                {
                    double inc = (v.to - v.from) / 10;
                    v.val += inc >= 1 ? inc : 1;
                    if (v.val > v.to)
                        v.val = v.from;
                    return v.val;
                }
            }
			return 0;
		}

        public static void HideAll()
        {
            lock (myUI)
            {
                foreach (KeyValuePair<string, Variable> v in myUI.variables)
                    if (v.Key != "Size" && v.Key != "Resolution")
                        v.Value.shown = false;
            }
        }

        public static DrWPF.Windows.Data.ObservableDictionary<string, Variable> Variables { get { return myUI.variables; } }
        public static IList<string> VariablesShown {
            get
            {
                List<string> vars = new List<string>();
                lock (myUI)
                {
                    foreach (KeyValuePair<string, Variable> v in Variables)
                        if (v.Value.shown)
                            vars.Add(v.Key);
                    return vars;
                }
            }
        }

																	    
	}
}
