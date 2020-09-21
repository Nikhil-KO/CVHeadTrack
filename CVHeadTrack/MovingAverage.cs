using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CVHeadTrack {

    class MovingAverage {

        private double[] buffer;
        private int index;

        public MovingAverage(int size) {
            this.buffer = new double[size];
            this.index = 0;
        }

        public void Add(double item) {
            this.buffer[this.index] = item;
            this.index = (this.index + 1) % this.buffer.Length;
        }

        public double Average() {
            double sum = 0;
            foreach (double item in buffer) {
                sum += item;
            }
            return (sum/buffer.Length);
        }

    }
}
