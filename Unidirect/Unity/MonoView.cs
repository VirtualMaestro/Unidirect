using System;
using Unidirect.Core.Events;
using Unidirect.Core.View;
using Unidirect.Helpers;
using UnityEngine;

namespace Unidirect.Unity
{
    public class MonoView<TView> : MonoBehaviour, IView where TView: MonoBehaviour
    {
        public static readonly Type Type = typeof(TView);
        
        public bool IsDisposed { get; private set; }
        public Type ViewType => Type;
        
        private void Awake()
        {
            IsDisposed = false;
            OnConstruct();
        }

        private void Start()
        {
            OnInitialize();
            EventBus<ViewReadyEvent>.Dispatch(EventBus<ViewReadyEvent>.GetEvent().Set(this));
        }
        
        /// <summary>
        /// User's code here.
        /// Method is invoked on construct phase. Uses Awake phase: World is already created but entity not. 
        /// </summary>
        protected virtual void OnConstruct()
        {
        }

        /// <summary>
        /// User's code here.
        /// Method is invoked on the init phase. Uses Start phase.
        /// Also this method will be invoked when object restores from the pool (if pool enable).
        /// </summary>
        protected virtual void OnInitialize()
        {
        }

        /// <summary>
        /// User's code here.
        /// Invokes when the script or gameobject is going to be disposed.
        /// </summary>
        protected virtual void OnDispose()
        {
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            _SendDisposeEvent();
            OnDispose();
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (IsDisposed)
                return;
            
            _SendDisposeEvent();
            OnDispose();
        }

        private void _SendDisposeEvent()
        {
            IsDisposed = true;
            EventBus<ViewDisposedEvent>.Dispatch(EventBus<ViewDisposedEvent>.GetEvent().Set(this));
        }

        public override string ToString()
        {
            return $"ViewMono of type: {ViewType}";
        }
    }
}