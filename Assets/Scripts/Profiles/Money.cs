using System;

namespace IdleOff.Profiles
{
    [Serializable]
    public struct Money
    {
        public int goldP;
        public int silverP;
        public int copperP;

        public Money(int goldP = 0, int silverP = 0, int copperP = 0)
        {
            this.goldP = Math.Max(0, goldP);
            this.silverP = Math.Max(0, silverP);
            this.copperP = Math.Max(0, copperP);
            Normalize();
        }

        public int TotalCopper => (goldP * 10000) + (silverP * 100) + copperP;

        public bool CanSubtract(Money value)
        {
            return TotalCopper >= value.TotalCopper;
        }

        public void Add(Money value)
        {
            SetFromCopper(TotalCopper + value.TotalCopper);
        }

        public bool TrySubtract(Money value)
        {
            if (!CanSubtract(value))
            {
                return false;
            }

            SetFromCopper(TotalCopper - value.TotalCopper);
            return true;
        }

        public static Money operator +(Money left, Money right)
        {
            return FromCopper(left.TotalCopper + right.TotalCopper);
        }

        public static Money operator -(Money left, Money right)
        {
            if (!left.CanSubtract(right))
            {
                throw new InvalidOperationException("Cannot subtract more money than the source contains.");
            }

            return FromCopper(left.TotalCopper - right.TotalCopper);
        }

        public static Money FromCopper(int copper)
        {
            var money = new Money();
            money.SetFromCopper(Math.Max(0, copper));
            return money;
        }

        private void SetFromCopper(int totalCopper)
        {
            goldP = totalCopper / 10000;
            totalCopper %= 10000;
            silverP = totalCopper / 100;
            copperP = totalCopper % 100;
        }

        private void Normalize()
        {
            SetFromCopper(TotalCopper);
        }
    }
}
