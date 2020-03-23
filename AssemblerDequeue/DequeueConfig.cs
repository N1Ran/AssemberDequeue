using System;
using Torch;

namespace AssemblerDequeue
{
    public class DequeueConfig : ViewModel
    {
        private bool _enabled = true;
        private int _delayInSeconds = 30;

        public bool Enabled
        {
            get => _enabled;
            set => SetValue(ref _enabled, value);
        }

        public int DelayInSeconds
        {
            get => _delayInSeconds;
            set => SetValue(ref _delayInSeconds, Math.Abs(value));
        }
    }
}