using System;
using UnityEngine;

namespace IdleOff.Profiles
{
    [Serializable]
    public sealed class MainStats
    {
        [SerializeField, Min(0)] private int str;
        [SerializeField, Min(0)] private int agi;
        [SerializeField, Min(0)] private int wis;
        [SerializeField, Min(0)] private int luck;

        public int GetSTR()
        {
            return str;
        }

        public void SetSTR(int value)
        {
            str = Mathf.Max(0, value);
        }

        public void AddSTR(int value)
        {
            SetSTR(str + value);
        }

        public void UpdateSTR()
        {
        }

        public int GetAGI()
        {
            return agi;
        }

        public void SetAGI(int value)
        {
            agi = Mathf.Max(0, value);
        }

        public void AddAGI(int value)
        {
            SetAGI(agi + value);
        }

        public void UpdateAGI()
        {
        }

        public int GetWIS()
        {
            return wis;
        }

        public void SetWIS(int value)
        {
            wis = Mathf.Max(0, value);
        }

        public void AddWIS(int value)
        {
            SetWIS(wis + value);
        }

        public void UpdateWIS()
        {
        }

        public int GetLUCK()
        {
            return luck;
        }

        public void SetLUCK(int value)
        {
            luck = Mathf.Max(0, value);
        }

        public void AddLUCK(int value)
        {
            SetLUCK(luck + value);
        }

        public void UpdateLUCK()
        {
        }

        public void Update()
        {
            UpdateSTR();
            UpdateAGI();
            UpdateWIS();
            UpdateLUCK();
        }
    }
}
