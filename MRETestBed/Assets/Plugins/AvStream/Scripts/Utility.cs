using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AvStreamPlugin
{
    public static class ExtensionUtilities
    {
        public static T EnsureComponent<T>(this Behaviour caller)
        {
            var component = caller.GetComponent<T>();
            if (component == null)
            {
                caller.enabled = false;
                throw new MissingComponentException(string.Format("Object of type {0} must have a component of type {1} attached",
                    caller.GetType(), typeof(T)));
            }
            return component;
        }
    }

    //--------------------------------------------------------------------------------------------------
    public class MovingAverage
    {
        private float[] window;
        private float total;
        private int numSamples;
        private int insertionIndex;
        private int period;

        public MovingAverage(int period)
        {
            this.period = period;
            window = new float[period];
            Clear();
        }

        public void AddSample(float sample)
        {
            // Filling the buffer
            if (this.numSamples < period)
            {
                this.window[this.insertionIndex] = sample;
                this.total += sample;

                this.numSamples++;
                this.insertionIndex++;
            }
            else
            {
                if (this.insertionIndex == period)
                {
                    this.insertionIndex = 0;
                }

                this.total -= this.window[this.insertionIndex];

                this.window[this.insertionIndex] = sample;
                this.total += sample;

                this.insertionIndex++;
            }
        }

        public void Clear()
        {
            this.total = 0;
            this.numSamples = 0;
            this.insertionIndex = 0;
        }

        public bool HasSamples()
        {
            return this.numSamples != 0;
        }

        public float Total
        {
            get { return this.total; }
        }

        public float Average
        {
            get
            {
                return this.numSamples > 0
                    ? (this.total / this.numSamples)
                    : 0;
            }
        }

        public int NumSamples
        {
            get { return this.numSamples; }
        }


    };
}
