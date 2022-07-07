using System;

namespace SoundAnalysis
{
    //комплексные числа
    struct ComplexNumber
    {
        public float Re;
        public float Im;

        public ComplexNumber(float re)
        {
            this.Re = re;
            this.Im = 0;
        }

        public ComplexNumber(float re, float im)
        {
            this.Re = re;
            this.Im = im;
        }

        public static ComplexNumber operator *(ComplexNumber n1, ComplexNumber n2)
        {
            return new ComplexNumber(n1.Re * n2.Re - n1.Im * n2.Im,
                n1.Im * n2.Re + n1.Re * n2.Im);
        }

        public static ComplexNumber operator +(ComplexNumber n1, ComplexNumber n2)
        {
            return new ComplexNumber(n1.Re + n2.Re, n1.Im + n2.Im);
        }

        public static ComplexNumber operator -(ComplexNumber n1, ComplexNumber n2)
        {
            return new ComplexNumber(n1.Re - n2.Re, n1.Im - n2.Im);
        }

        public static ComplexNumber operator -(ComplexNumber n)
        {
            return new ComplexNumber(-n.Re, -n.Im);
        }

        public static implicit operator ComplexNumber(float n)
        {
            return new ComplexNumber((float)n, 0);
        }

        public ComplexNumber PoweredE()
        {
            float e = (float)Math.Exp(Re);
            return new ComplexNumber((float)(e * Math.Cos(Im)), (float)(e * Math.Sin(Im)));
        }

        public double Power2()
        {
            return Re * Re - Im * Im;
        }

        public double AbsPower2()
        {
            return Re * Re + Im * Im;
        }

        public override string ToString()
        {
            return String.Format("{0}+i*{1}", Re, Im);
        }
    }
}
