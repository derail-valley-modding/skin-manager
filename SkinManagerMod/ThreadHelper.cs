using DV.Utils;
using System;
using System.Collections.Generic;

namespace SkinManagerMod
{
    internal class ThreadHelper : SingletonBehaviour<ThreadHelper>
    {
        public new static string AllowAutoCreate() => "[SM_ThreadHelper]";

        private readonly Queue<Action> _toExecute = new Queue<Action>();

        public void EnqueueAction(Action action)
        {
            _toExecute.Enqueue(action);
        }

        public void Update()
        {
            if (_toExecute.Count == 0) return;

            if (_toExecute.TryDequeue(out Action action))
            {
                action?.Invoke();
            }
        }

        protected override void OnDestroy()
        {
            while (_toExecute.TryDequeue(out Action action))
            {
                action?.Invoke();
            }

            base.OnDestroy();
        }
    }
}
