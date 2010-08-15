using System;
using System.ComponentModel;

namespace Terry
{

    public class SliderAttribute : IComparable, IEquatable<SliderAttribute>
    {
        public SliderAttribute() {}
        public SliderAttribute(string name) { Name = name; }

        public string Name { get; set; }
        protected object _value;
        public object Value { get { return _value; } }
        virtual public void Bump() { }

        protected bool goingUp = true;

        #region IComparable Members

        public int CompareTo(object obj)
        {
            if (obj != null && obj is SliderAttribute)
                return this.Name.CompareTo((obj as SliderAttribute).Name);
            else return -1;
        }

        #endregion

        #region IEquatable<SliderAttribute> Members

        public bool Equals(SliderAttribute other)
        {
            if (other != null && other is SliderAttribute)
                return this.Name.Equals((other as SliderAttribute).Name);
            else return false;
        }

        #endregion
    }
    public class SliderInt : SliderAttribute, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public SliderInt(string name, int from, int to, int def) { Name = name; _value = def; From = from; To = to; }
        public int Val { 
            get { return (int)_value; } 
            set { 
                _value = value;
                if (null != this.PropertyChanged)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("Val"));
                }
            } 
        }
        public int From { get; set; }
        public int To { get; set; }
        override public void Bump()
        {
            Val += Math.Min((To - From) / 10, 1) * (goingUp ? 1 : -1);
            if (Val >= To)
                goingUp = false;
            else if(Val <= From)
                goingUp = true;

        }
    }
    public class SliderDouble : SliderAttribute, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public SliderDouble(string name, double from, double to, double def) { Name = name; _value = def; From = from; To = to; }
        public double Val
        {
            get { return (double)_value; }
            set
            {
                _value = value;
                if (null != this.PropertyChanged)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("Val"));
                }
            }
        }
        public double From { get; set; }
        public double To { get; set; }
        override public void Bump()
        {
            Val += Math.Min((To - From) / 20, 1) * (goingUp ? 1.0 : -1.0);
            if (Val > To)
                goingUp = false;
            else if (Val < From)
                goingUp = true;
        }
    }
    public class SliderText : SliderAttribute
    {
        public SliderText(string name, string def) { Name = name; _value = def; }
        public string Val { get { return (string)_value; } set { _value = value; } }
    }
}