using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Unidirect.Helpers
{
    /// <summary>
    /// Coroutine Manager.
    /// </summary>
    public sealed class CoM : Singleton<CoM>
    {
        /// <summary>
        /// Start classic Unity's coroutine.
        /// </summary>
        public static Coroutine Run(IEnumerator coroutine)
        {
            return I._RunCoroutine(coroutine);
        }

        /// <summary>
        /// Stops classic Unity's coroutine.
        /// </summary>
        public static void Stop(Coroutine coroutine)
        {
            I._Stop(coroutine);
        }

        /// <summary>
        /// As a param uses reference to a coroutine method (not invocation result).
        /// </summary>
        public static void Run(Func<IEnumerator> func)
        {
            I._RunCoroutine(func);
        }

        /// <summary>
        /// As a param uses reference to a coroutine method.
        /// </summary>
        public static void Stop(Func<IEnumerator> func)
        {
            I._Stop(func);
        }
        
        public static void Run(ICJob job, bool skipFrame = false)
        {
            I._RunJob((CoroutineJob) job, skipFrame);
        }

        public static ICJob NewJob(Action method, bool isPaused = false)
        {
            return CoroutineJob.Get(method, isPaused);
        }

        /// <summary>
        /// Stops all running coroutines. 
        /// </summary>
        public static void StopAll()
        {
            I._StopAll();
        }

        /// <summary>
        /// Invokes given function every given amount of seconds.
        /// The process will start in the next frame.
        /// </summary>
        /// <param name="func">Function to invoke.</param>
        /// <param name="seconds">Delay between invocations in seconds.</param>
        ///
        /// <param name="skipFrame">If 'true' the given function will be called starting from the next frame.</param>
        public static ICJob EverySeconds(Action func, float seconds, bool skipFrame = false)
        {
            var job = CoroutineJob.Get(func).InSeconds(seconds);

            return I._RunJob((CoroutineJob) job, skipFrame);
        }

        /// <summary>
        /// Invokes given function every given amount of frames.
        /// </summary>
        /// <param name="func">Function to invoke.</param>
        /// <param name="frames">Delay between invocations in frames.</param>
        /// <param name="skipFrame">If 'true' the given function will be called starting from the next frame.</param>
        public static ICJob EveryFrames(Action func, int frames, bool skipFrame = false)
        {
            var job = CoroutineJob.Get(func).InFrames(frames);
            
            return I._RunJob((CoroutineJob) job, skipFrame);
        }

        /// <summary>
        /// Invokes given function one time after a delay in a given amount of seconds. 
        /// </summary>
        public static ICJob OnceSeconds(Action func, float seconds)
        {
            var job = CoroutineJob.Get(func).InSeconds(seconds).Times(1);
            return I._RunJob((CoroutineJob) job, false);
        }
        
        /// <summary>
        /// Invokes given function one time after a delay in a given amount of frames.
        /// </summary>
        public static ICJob OnceFrames(Action func, int frames)
        {
            var job = CoroutineJob.Get(func).InFrames(frames).Times(1);
            return I._RunJob((CoroutineJob) job, false);
        }

        /// <summary>
        /// Invokes given function once on the next frame. 
        /// </summary>
        public static ICJob NextFrame(Action func)
        {
            var job = CoroutineJob.Get(func).Times(1);
            return I._RunJob((CoroutineJob) job, true);
        }

        private Dictionary<Func<IEnumerator>, Coroutine> _coroutinesMap;
        private LinkedList<CoroutineJob> _jobs;
        private List<CoroutineJob> _skipFrameJobs;

        private void Awake()
        {
            _coroutinesMap = new Dictionary<Func<IEnumerator>, Coroutine>();
            _jobs = new LinkedList<CoroutineJob>();
            _skipFrameJobs = new List<CoroutineJob>(4);
        }
        
        private Coroutine _RunCoroutine(IEnumerator coroutine)
        {
            return StartCoroutine(coroutine);
        }
        
        private void _Stop(Coroutine coroutine)
        {
            StopCoroutine(coroutine);
        }

        private void _RunCoroutine(Func<IEnumerator> func)
        {
            if (!_coroutinesMap.TryGetValue(func, out var coroutine))
            {
                coroutine = I._RunCoroutine(func.Invoke());
                _coroutinesMap.Add(func, coroutine);
            }
        }
        
        private void _Stop(Func<IEnumerator> func)
        {
            if (_coroutinesMap.TryGetValue(func, out var coroutine))
            {
                I.StopCoroutine(coroutine);
                _coroutinesMap.Remove(func);
            }
        }

        private void _StopAll()
        {
            StopAllCoroutines();
            
            _coroutinesMap.Clear();
            _DisposeAllJobs();
        }

        private ICJob _RunJob(CoroutineJob job, bool skipFrame)
        {
            if (skipFrame)
                _skipFrameJobs.Add(job);
            else
                _jobs.AddLast(job.Node);
            
            _RunCoroutine(_CoroutineLoop);

            return job;
        }

        private IEnumerator _CoroutineLoop()
        {
            while (_jobs.Count > 0 || _skipFrameJobs.Count > 0)
            {
                var nextNode = _jobs.First;
                var deltaTime = Time.deltaTime;
                
                while (nextNode != null && _jobs.Count > 0) // all jobs were disposed during the traversal
                {
                    var job = nextNode.Value;
                    nextNode = nextNode.Next;
                    
                    if (!job.IsPaused && job.Process(deltaTime))
                        job.Dispose();
                }

                // 
                if (_skipFrameJobs.Count > 0)
                {
                    foreach (var job in _skipFrameJobs)
                        _jobs.AddLast(job.Node);
                    
                    _skipFrameJobs.Clear();
                }
                
                yield return null;
            }
        
            _Stop(_CoroutineLoop);
        }

        private void _DisposeAllJobs()
        {
            while (_jobs.Count > 0)
                _jobs.Last.Value.Dispose();
        }

        public interface ICJob
        {
            /// <summary>
            /// Invokes when job has finished its job and disposed 
            /// </summary>
            event Action OnComplete;
            bool IsDisposed { get; }
            bool IsPaused { set; get; }

            /// <summary>
            /// </summary>
            public ICJob InFrames(int frames);

            /// <summary>
            /// </summary>
            public ICJob InSeconds(float seconds);
            
            public ICJob Times(int times);

            /// <summary>
            /// Wait given amount of seconds before coroutine starts.
            /// </summary>
            public ICJob DelaySeconds(float seconds);

            /// <summary>
            /// Wait given amount of frames before coroutine starts.
            /// </summary>
            public ICJob DelayFrames(int frames);

            void Dispose();
        }
        
        private class CoroutineJob : ICJob
        {
            public event Action OnComplete;

            public readonly LinkedListNode<CoroutineJob> Node;

            private float _initSeconds;
            private int _initFrames;
            private float _seconds;
            private int _frames;
            private float _delaySeconds;    // delay before start in seconds
            private int _delayFrames;           // delay before start in frames
            private bool _hasDelay;
            private int _times;
            private Action _method;

            public bool IsPaused { set; get; }
            public bool IsDisposed => Node.List == null;

            private CoroutineJob(Action method, bool isPaused = false)
            {
                Node = new LinkedListNode<CoroutineJob>(this);
                _Reset();
                Set(method, isPaused);
            }

            private void Set(Action method, bool isPaused = false)
            {
                _method = method;
                
                IsPaused = isPaused;
            }

            public ICJob InFrames(int frames)
            {
                _initFrames = frames;
                _frames = _initFrames;
                
                return this;
            }

            public ICJob InSeconds(float seconds)
            {
                _initSeconds = seconds;
                _seconds = _initSeconds;
                
                return this;
            }

            public ICJob Times(int times)
            {
                _times = times;
                return this;
            }

            public ICJob DelaySeconds(float seconds)
            {
                _delaySeconds = seconds;
                _hasDelay = _delaySeconds > 0;
                return this;
            }

            public ICJob DelayFrames(int frames)
            {
                _delayFrames = frames;
                _hasDelay = _delayFrames > 0;
                return this;
            }

            // Returns 'true' if a job should be disposed
            public bool Process(float timeStep)
            {
                if (_hasDelay)
                {
                    _delaySeconds -= timeStep;
                    --_delayFrames;
                    
                    _hasDelay = _delaySeconds > 0 || _delayFrames > 0;
                    
                    return false;
                }
                
                _seconds -= timeStep;
                --_frames;
                
                if (_seconds > 0 || _frames > 0) 
                    return false;
                
                _method();
                    
                _seconds = _initSeconds;
                _frames = _initFrames;

                if (_times < int.MaxValue)
                {
                    --_times;
                    _times = _times > 0 ? _times : 0;
                }

                return _times == 0;
            }

            public void Dispose()
            {
                if (Node.List == null)
                    return;
                
                Node.List.Remove(Node);

                _method = null;
                
                Put(this);
                
                OnComplete?.Invoke();
            }

            private void _Reset()
            {
                _initSeconds = -1;
                _initFrames = -1;
                _seconds = -1;
                _frames = -1;
                _delaySeconds = 0;
                _delayFrames = 0;
                _hasDelay = false;
                IsPaused = false;
                _times = int.MaxValue;
                _method = null;
            }

            // *** Pool ***//
            private static readonly Stack<CoroutineJob> Pool = new(10);

            public static CoroutineJob Get(Action method, bool isPaused = false)
            {
                if (Pool.Count == 0) 
                    return new CoroutineJob(method, isPaused);
                
                var job = Pool.Pop();
                job.Set(method, isPaused);
                return job;
            }
            
            public static void Clear()
            {
                Pool.Clear();
            }
            
            private static void Put(CoroutineJob coroutineJob)
            {
                coroutineJob._Reset();
                Pool.Push(coroutineJob);
            }
        }
    }
    
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static readonly Lazy<T> LazyInstance = new(_CreateSingleton, LazyThreadSafetyMode.PublicationOnly);

        protected static T I => LazyInstance.Value;

        private static T _CreateSingleton()
        {
            var existingInstance = FindObjectsByType<T>(FindObjectsSortMode.None);
            
            if (existingInstance == null || existingInstance.Length == 0)
            {
                var ownerObject = new GameObject($"{typeof(T).Name} (singleton)");
                var instance = ownerObject.AddComponent<T>();
                DontDestroyOnLoad(ownerObject);
                return instance;
            }

            return existingInstance[0];
        }
    }
}