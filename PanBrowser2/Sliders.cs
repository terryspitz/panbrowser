
using System;
namespace Terry
{

    public class SliderAttribute : IComparable, IEquatable<SliderAttribute>
    {
        public SliderAttribute() {}
        public SliderAttribute(string name) { Name = name; }

        public string Name { get; set; }
        public object Value { get; set; }
        virtual public void Bump() { }

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
    public class SliderInt : SliderAttribute
    {
        public SliderInt(string name, int from, int to, int def) { Name = name; Value = def; From = from; To = to; }
        public int Val { get { return (int)Value; } set { Value = value; } }
        public int From { get; set; }
        public int To { get; set; }
        override public void Bump()
        {
            Val += Math.Min((To - From) / 10, 1);
            if (Val > To)
                Val = From;
        }
    }
    public class SliderText : SliderAttribute
    {
        public SliderText(string name, string def) { Name = name; Value = def; }
        public string Val { get { return (string)Value; } set { Value = value; } }
    }
    public class SliderDouble : SliderAttribute
    {
        public SliderDouble(string name, double from, double to, double def) { Name = name; Value = def; From = from; To = to; }
        public double Val { get { return (double)Value; } set { Value = value; } }
        public double From { get; set; }
        public double To { get; set; }
        override public void Bump()
        {
            Val += Math.Min((To - From) / 20, 1);
            if (Val > To)
                Val = From;
        }
    }
}