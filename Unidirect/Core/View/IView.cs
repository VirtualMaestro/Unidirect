using System;
using UnityEngine;

namespace Unidirect.Core.View
{
    public interface IView<TView> : IView where TView: MonoBehaviour
    { }

    public interface IView
    {
        public Type ViewType { get; }
    }
}