﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mond.Binding;

namespace Mond.Libraries.Async
{
    [MondClass("Async")]
    internal class AsyncClass
    {
        private readonly Scheduler _scheduler;
        private readonly TaskFactory _factory;
        private int _activeTasks;

        private Queue<Exception> _exceptions;

        private AsyncClass()
        {
            _scheduler = new Scheduler();
            _factory = new TaskFactory(_scheduler);
            _activeTasks = 0;

            _exceptions = new Queue<Exception>();
        }

        public static MondValue Create()
        {
            MondValue prototype;
            MondClassBinder.Bind<AsyncClass>(out prototype);

            var instance = new AsyncClass();

            var obj = new MondValue(MondValueType.Object);
            obj.UserData = instance;
            obj.Prototype = prototype;
            obj.Lock();

            return obj;
        }

        [MondFunction("start")]
        public void Start(MondState state, MondValue value)
        {
            if (value.Type == MondValueType.Function)
                value = state.Call(value);

            var getEnumerator = value["getEnumerator"];

            if (getEnumerator.Type != MondValueType.Function)
                throw new MondRuntimeException("Task objects must define getEnumerator");

            var enumerator = state.Call(getEnumerator);

            _factory.StartNew(async () =>
            {
                try
                {
                    await AsyncUtil.RunMondTask(state, enumerator);
                }
                catch (Exception e)
                {
                    lock (_exceptions)
                        _exceptions.Enqueue(e);
                }
                finally
                {
                    Interlocked.Decrement(ref _activeTasks);
                }
            });

            Interlocked.Increment(ref _activeTasks);
        }

        [MondFunction("run")]
        public bool Run()
        {
            Exception ex = null;

            lock (_exceptions)
            {
                if (_exceptions.Count > 0)
                    ex = _exceptions.Dequeue();
            }

            if (ex != null)
            {
                var sb = new StringBuilder();
                sb.AppendLine("Unhandled error in task:");
                sb.Append(ex.Message);

                throw new MondRuntimeException(sb.ToString(), ex);
            }

            _scheduler.Run();

            lock (_exceptions)
                return _activeTasks > 0 || _exceptions.Count > 0;
        }

        [MondFunction("runToCompletion")]
        public void RunToCompletion()
        {
            while (Run())
            {
                Thread.Sleep(1);
            }
        }
    }
}
