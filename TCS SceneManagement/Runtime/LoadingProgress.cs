using System;
namespace TCS.Bootstrapper {
    public class LoadingProgress : IProgress<float> {
        public event Action<float> Progressed;

        const float RATIO = 1f;

        public void Report(float value) {
            Progressed?.Invoke(value / RATIO);
        }
    }
}