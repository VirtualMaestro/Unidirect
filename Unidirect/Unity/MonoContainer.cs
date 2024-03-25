using System;
using Unidirect.Core;
using UnityEngine;

namespace Unidirect.Unity
{
    public class MonoContainer<TController, TModel> : MonoBehaviour where TController: Controller<TModel>, new() where TModel : class, new()
    {
        public bool isDontDestroyOnLoad = true;
        
        private TController _controller;

        protected virtual TController CreateController()
        {
            return Activator.CreateInstance<TController>();
        }
        
        private void Awake()
        {
            if (isDontDestroyOnLoad)
                DontDestroyOnLoad(this);
            
            _controller = CreateController();  
        }

        private void Start()
        {
            _controller.Initialize();
        }

        private void Update()
        {
            _controller.Update();
        }

        private void FixedUpdate()
        {
            _controller.FixedUpdate();
        }

        private void OnDestroy()
        {
            _controller?.Dispose();
            _controller = null;
        }
    }
}