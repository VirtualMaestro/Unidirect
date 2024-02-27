using System;

namespace Unidirect.Core.Logic
{
    public sealed class Response
    {
        public bool IsModelUpdated;
        public ResponseCommand Command;
        public int ActionID;
        public Type ActionType;
    }
}